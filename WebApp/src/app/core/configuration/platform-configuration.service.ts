import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { catchError, forkJoin, of, switchMap, tap } from 'rxjs';
import { CONFIGURATION_CATEGORY } from './audit-configuration.catalogs';
import { PLATFORM_CONFIGURATION_CATALOGS } from './platform-configuration.catalogs';
import {
  ConfigurationApiService,
  type ResolvedConfigurationCategory,
} from './configuration-api.service';
import {
  humaniseConfigurationId,
  normaliseConfigurationId,
  type ConfigurationOption,
  type ConfigurationValueDescriptor,
} from './configuration-catalog';

/** One category as currently resolved, with an id index so lookups are not linear scans. */
interface CategoryState {
  /** Values in display order. */
  readonly values: readonly ConfigurationValueDescriptor[];
  /** Keyed by {@link normaliseConfigurationId} so casing differences cannot miss. */
  readonly byId: ReadonlyMap<string, ConfigurationValueDescriptor>;
  /** Version of the loaded catalog. Empty while running on the seed. */
  readonly version: string;
}

/**
 * Resolves platform configuration for the current organization.
 *
 * <p>
 * **The seam between Administration and the workspace.** Enterprise Administration configures
 * behaviour as metadata; the Audit workspace consumes it. Consumers read from here and never from a
 * literal, so when Administration changes a status label, a severity or an audit type, nothing
 * downstream is edited or redeployed.
 * </p>
 *
 * <p>
 * **Seeds, not fallback branches.** {@link PLATFORM_CONFIGURATION_CATALOGS} supplies the starting
 * content of the same store the API writes into. There is no `if (configured) … else …` in any
 * consumer, so the configured and unconfigured paths cannot drift — which is what makes it safe for
 * most categories to have no server-side rows yet.
 * </p>
 *
 * <p>
 * **Read-mostly, so it lives in signals.** Configuration is small and changes rarely; holding it in
 * signals lets a screen bind to it directly and re-render when an administrator's edit propagates,
 * instead of every consumer fetching and caching its own copy.
 * </p>
 */
@Injectable({ providedIn: 'root' })
export class PlatformConfigurationService {
  private readonly api = inject(ConfigurationApiService);

  private readonly store = signal<ReadonlyMap<string, CategoryState>>(seedStore());

  /** In-flight/complete guard — configuration is loaded once per session, not per consumer. */
  private loaded = false;

  /**
   * Derived signals are memoised per category (and per attribute) so a component field like
   * `readonly severityOptions = config.options(FINDING_SEVERITY)` is a stable reference. Rebuilding
   * the array on every change-detection pass would defeat `OnPush` on every screen that binds one.
   */
  private readonly derived = new Map<string, Signal<readonly ConfigurationValueDescriptor[]>>();
  private readonly derivedOptions = new Map<string, Signal<readonly ConfigurationOption[]>>();

  /** True once at least one category has been resolved from the API for this organization. */
  readonly isConfigurationLoaded = computed(() =>
    [...this.store().values()].some((c) => c.version !== ''),
  );

  /**
   * True when this organization's configuration for a category came from the API rather than the
   * seed. Lets Administration show which domains a tenant has actually customised.
   */
  isCategoryConfigured(code: string): boolean {
    return this.state(code).version !== '';
  }

  // ---------------------------------------------------------------------------------------------
  // Reading configuration
  // ---------------------------------------------------------------------------------------------

  /** Every value in a category, in display order. */
  values(code: string): Signal<readonly ConfigurationValueDescriptor[]> {
    return this.memoise(this.derived, code, () => computed(() => this.state(code).values));
  }

  /**
   * Values whose boolean `attribute` is true.
   *
   * This is how a consumer expresses "the statuses a user may filter by" without naming any of
   * them. A category that has not yet declared the attribute yields an empty list, which is the
   * honest answer — an unconfigured capability is absent, not silently all-inclusive.
   */
  valuesWith(code: string, attribute: string): Signal<readonly ConfigurationValueDescriptor[]> {
    return this.memoise(this.derived, `${code}#${attribute}`, () =>
      computed(() => this.state(code).values.filter((v) => v.attributes[attribute] === true)),
    );
  }

  /** Every value as `{ value, label }` — the shape the workspace's `<select>` elements bind to. */
  options(code: string): Signal<readonly ConfigurationOption[]> {
    return this.memoise(this.derivedOptions, code, () => {
      const source = this.values(code);
      return computed(() => source().map(toOption));
    });
  }

  /** Values whose boolean `attribute` is true, as `{ value, label }`. */
  optionsWith(code: string, attribute: string): Signal<readonly ConfigurationOption[]> {
    return this.memoise(this.derivedOptions, `${code}#${attribute}`, () => {
      const source = this.valuesWith(code, attribute);
      return computed(() => source().map(toOption));
    });
  }

  /** The descriptor for a value, or `null` when the category does not define it. */
  descriptor(code: string, id: string | null | undefined): ConfigurationValueDescriptor | null {
    if (!id) return null;
    return this.state(code).byId.get(normaliseConfigurationId(String(id))) ?? null;
  }

  /**
   * Display label for a value.
   *
   * Unknown values are humanised rather than dropped, so a value added server-side is visible
   * instead of rendering an empty cell — the failure mode that blanked the Type column across two
   * screens (DEF-004, DEF-013).
   */
  label(code: string, id: string | null | undefined, fallback = '—'): string {
    if (id === null || id === undefined || id === '') return fallback;

    const match = this.descriptor(code, String(id));
    if (match) return match.label;

    return humaniseConfigurationId(String(id)) || fallback;
  }

  /**
   * Label for a value identified by its backend enum ordinal, falling back to code lookup.
   *
   * `FindingRaisedBy` is serialised with `.ToString()`, so a row holding a value outside the enum
   * arrives as the literal string `"0"` and used to reach the user as a bare `0` in the SOURCE
   * column. Resolving numerics through the catalog's `ordinal` attribute keeps that in one place.
   */
  labelByOrdinalOrCode(
    code: string,
    raw: string | number | null | undefined,
    fallback = '—',
  ): string {
    if (raw === null || raw === undefined || raw === '') return fallback;

    const asNumber = typeof raw === 'number' ? raw : Number(raw);
    if (Number.isFinite(asNumber) && String(raw).trim() !== '') {
      const byOrdinal = this.state(code).values.find((v) => v.attributes['ordinal'] === asNumber);
      // An out-of-range ordinal is data corruption, not a new value — showing the fallback is
      // better than showing the number.
      return byOrdinal ? byOrdinal.label : fallback;
    }

    return this.label(code, String(raw), fallback);
  }

  /**
   * A value's `attribute`, or `fallback` when the value or attribute is not configured.
   *
   * The `fallback` type drives the return type, so a caller asking for a CSS class gets a string and
   * a caller asking for a day count gets a number without casting at the call site.
   */
  attribute<T extends string | number | boolean>(
    code: string,
    id: string | null | undefined,
    key: string,
    fallback: T,
  ): T {
    const value = this.descriptor(code, id)?.attributes[key];
    return typeof value === typeof fallback ? (value as T) : fallback;
  }

  /**
   * `attribute`, resolving an unknown id to the category's designated fallback value.
   *
   * SLA state and routing confidence both render *something* for a value they do not recognise —
   * previously the final `else` of an `if` ladder, which meant a genuinely new SLA state was
   * displayed as "closed". Naming the fallback in the catalog makes that choice configurable and
   * visible instead of implicit in a branch.
   */
  attributeOrFallback(
    code: string,
    id: string | null | undefined,
    key: string,
    fallback = '',
  ): string {
    const match = this.descriptor(code, id) ?? this.fallbackDescriptor(code);
    const value = match?.attributes[key];
    return typeof value === 'string' ? value : fallback;
  }

  /** The value a category designates as its catch-all, if any. */
  private fallbackDescriptor(code: string): ConfigurationValueDescriptor | null {
    return this.state(code).values.find((v) => v.attributes['fallback'] === true) ?? null;
  }

  /** Position of a value within its category's lifecycle, or 0 when it is not a lifecycle step. */
  lifecycleIndex(code: string, id: string | null | undefined, attribute = 'lifecycleStep'): number {
    const steps = this.state(code).values.filter((v) => v.attributes[attribute] === true);
    const normalised = id ? normaliseConfigurationId(String(id)) : '';
    const index = steps.findIndex((s) => normaliseConfigurationId(s.id) === normalised);
    return index >= 0 ? index : 0;
  }

  // ---------------------------------------------------------------------------------------------
  // Audit types — the accessors Planning established, now reading the shared store
  // ---------------------------------------------------------------------------------------------

  /** Every configured audit type, in display order. */
  readonly allAuditTypes = this.values(CONFIGURATION_CATEGORY.auditType);

  /** Types a planner may schedule. */
  readonly schedulableAuditTypes = this.valuesWith(
    CONFIGURATION_CATEGORY.auditType,
    'selectableWhenScheduling',
  );

  /** Types belonging to the continuous surveillance cycle. */
  readonly surveillanceAuditTypes = this.valuesWith(
    CONFIGURATION_CATEGORY.auditType,
    'surveillance',
  );

  /** Display label for an audit type. */
  auditTypeLabel(id: string | null | undefined, fallback = '—'): string {
    return this.label(CONFIGURATION_CATEGORY.auditType, id, fallback);
  }

  /** True when `id` names a configured surveillance audit type. */
  isSurveillanceAuditType(id: string | null | undefined): boolean {
    return this.attribute(CONFIGURATION_CATEGORY.auditType, id, 'surveillance', false);
  }

  // ---------------------------------------------------------------------------------------------
  // Loading
  // ---------------------------------------------------------------------------------------------

  /**
   * Load configuration from the Enterprise Configuration API.
   *
   * <p>
   * Asks which categories exist before asking for any of them, so a platform serving one configured
   * category costs two requests rather than one per registered catalog. Categories the platform
   * does not report stay on their seed and cost nothing.
   * </p>
   *
   * <p>
   * Idempotent: the first caller triggers the load and later callers are no-ops, so a screen with
   * several configuration-driven controls does not produce several rounds of requests. Consumers
   * read signals and update reactively when responses land, so none of them needs to await this.
   * </p>
   *
   * <p>
   * On failure the seeds stay in place. An unreachable configuration service degrades to
   * platform-default behaviour rather than empty dropdowns — the difference between a screen that
   * still works and one that looks broken.
   * </p>
   */
  load(force = false): void {
    if (this.loaded && !force) return;
    this.loaded = true;

    this.api
      .getCategoryCodes()
      .pipe(
        switchMap((codes) => {
          const available = new Set(codes.map(normaliseConfigurationId));
          const wanted = PLATFORM_CONFIGURATION_CATALOGS.filter((c) =>
            available.has(normaliseConfigurationId(c.code)),
          );

          if (wanted.length === 0) return of([] as (ResolvedConfigurationCategory | null)[]);
          return forkJoin(wanted.map((c) => this.api.getCatalog(c.code)));
        }),
        // One write for the whole load: a per-category write would re-render every bound screen
        // once per category for no benefit.
        tap((results) => this.apply(results)),
        catchError(() => of(null)),
      )
      .subscribe();
  }

  /** Discard the cached catalogs and reload — used after an administrator edits configuration. */
  refresh(): void {
    this.load(true);
  }

  /**
   * Adopt a category the server has just re-resolved, typically after a write.
   *
   * <p>
   * The write endpoints return the whole category rather than an acknowledgement, so an
   * administrator's save can propagate to every bound screen from the response itself. Reloading
   * instead would cost a round trip per save and would briefly show stale values in between.
   * </p>
   *
   * <p>
   * `null` means the write failed or the category does not exist; the store is left alone. A
   * category that resolves to **no values** is the opposite — the successful result of restoring the
   * last override — and returns that category to its platform seed. Treating those two the same
   * would leave a restored category still showing the values the administrator just withdrew.
   * </p>
   */
  applyCategory(category: ResolvedConfigurationCategory | null): void {
    if (!category) return;

    if (category.values.length === 0) {
      this.restoreSeed(category.code);
      return;
    }

    this.apply([category]);
  }

  /** Return one category to its seeded content, as an unconfigured tenant sees it. */
  private restoreSeed(code: string): void {
    const catalog = PLATFORM_CONFIGURATION_CATALOGS.find((c) => c.code === code);
    if (!catalog) return;

    const next = new Map(this.store());
    next.set(code, toState(catalog.seed, ''));
    this.store.set(next);
  }

  /**
   * Restore the seeded catalogs. Used on logout and when switching organization — configuration is
   * tenant-scoped, so carrying it across a context switch would show one tenant another's values.
   */
  reset(): void {
    this.store.set(seedStore());
    this.loaded = false;
  }

  /** Replace resolved categories, ignoring those that came back unresolved. */
  private apply(results: readonly (ResolvedConfigurationCategory | null)[]): void {
    const resolved = results.filter((r): r is ResolvedConfigurationCategory => r !== null);
    if (resolved.length === 0) return;

    const current = this.store();
    // Version comparison, not a blind write: an unchanged catalog must not churn signals and
    // re-render every consumer.
    const changed = resolved.filter((r) => current.get(r.code)?.version !== r.version);
    if (changed.length === 0) return;

    const next = new Map(current);
    for (const category of changed) {
      next.set(category.code, toState(category.values, category.version));
    }
    this.store.set(next);
  }

  /** A category's current state, or an empty one when the code is not registered. */
  private state(code: string): CategoryState {
    return this.store().get(code) ?? EMPTY_CATEGORY;
  }

  private memoise<T>(
    cache: Map<string, Signal<T>>,
    key: string,
    create: () => Signal<T>,
  ): Signal<T> {
    const existing = cache.get(key);
    if (existing) return existing;
    const created = create();
    cache.set(key, created);
    return created;
  }
}

const EMPTY_CATEGORY: CategoryState = { values: [], byId: new Map(), version: '' };

function toOption(value: ConfigurationValueDescriptor): ConfigurationOption {
  return { value: value.id, label: value.label };
}

/** Sort and index once at write time so every read is cheap. */
function toState(values: readonly ConfigurationValueDescriptor[], version: string): CategoryState {
  const sorted = [...values].sort((a, b) => a.sortOrder - b.sortOrder);
  const byId = new Map(sorted.map((v) => [normaliseConfigurationId(v.id), v]));
  return { values: sorted, byId, version };
}

function seedStore(): ReadonlyMap<string, CategoryState> {
  return new Map(
    PLATFORM_CONFIGURATION_CATALOGS.map((catalog) => [catalog.code, toState(catalog.seed, '')]),
  );
}
