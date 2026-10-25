import { Injectable } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { BaseApiService } from '../../shared/services/base-api.service';
import type {
  ConfigurationAttributes,
  ConfigurationValueDescriptor,
} from './configuration-catalog';

/** A configurable value as returned by the Enterprise Configuration API. */
export interface ConfigurationValueDto {
  code: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  /** True when this is the tenant's override rather than the platform default. */
  isTenantOverride: boolean;
  /** Domain-specific attributes, typed by the consumer. */
  metadata?: string | null;
  parentCode?: string | null;
}

/** A resolved configuration category for the caller's organization. */
export interface ConfigurationCategoryDto {
  code: string;
  name: string;
  description?: string | null;
  isTenantConfigurable: boolean;
  /** Hash of the resolved values — the cache key, not a timestamp. */
  version: string;
  values: ConfigurationValueDto[];
}

/**
 * An administrator's edit to a single configuration value.
 *
 * Mirrors the server's `ConfigurationOverrideRequest`. Every field but `code` is optional: an
 * omitted field keeps its current value, so an edit that only renames a value does not have to
 * resend — and risk silently rewriting — its ordering, activity and metadata.
 */
export interface ConfigurationOverrideRequest {
  /** Which value is being overridden. Required; everything else is a change to it. */
  code: string;
  name?: string;
  sortOrder?: number;
  isActive?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  /** Domain attributes as a JSON string, matching the storage column. */
  metadata?: string;
}

/** A category resolved into the descriptor shape consumers read. */
export interface ResolvedConfigurationCategory {
  readonly code: string;
  readonly version: string;
  readonly values: readonly ConfigurationValueDescriptor[];
}

/**
 * Client for the Enterprise Configuration API.
 *
 * <p>
 * The organization is resolved by the Gateway from the caller's token, so no tenant identifier is
 * sent from here — the client cannot ask for another tenant's configuration even by accident.
 * </p>
 *
 * <p>
 * Every read degrades to `null` rather than throwing. Configuration is infrastructure: a screen
 * that cannot reach it should fall back to its seed and stay usable, not fail. Deciding what to do
 * with `null` belongs to {@link PlatformConfigurationService}, which owns the seeds.
 * </p>
 */
@Injectable({ providedIn: 'root' })
export class ConfigurationApiService extends BaseApiService {
  /** Category codes the platform knows about. */
  getCategoryCodes(): Observable<string[]> {
    return this.get<string[]>('/configuration/categories').pipe(catchError(() => of([])));
  }

  /** A resolved category, or `null` when unreachable or not configured. */
  getCategory(code: string): Observable<ConfigurationCategoryDto | null> {
    return this.get<ConfigurationCategoryDto>(
      `/configuration/categories/${encodeURIComponent(code)}`,
    ).pipe(catchError(() => of(null)));
  }

  /**
   * A category mapped into the shape consumers already use.
   *
   * <p>
   * The mapping lives here rather than in the consumer so the wire format stays a detail of this
   * service: if `metadata` becomes typed columns, nothing downstream changes.
   * </p>
   *
   * <p>
   * Returns `null` for an empty category as well as an unreachable one. Both mean "this tenant has
   * no configuration for this domain", and the seed is a better answer than an empty picker.
   * </p>
   */
  getCatalog(code: string): Observable<ResolvedConfigurationCategory | null> {
    return this.getCategory(code).pipe(map(toResolved));
  }

  /**
   * Create or update this organization's override of one value.
   *
   * <p>
   * Every field but `code` is optional, matching the server: an omitted field keeps its current
   * value, so renaming a value need not resend its ordering and metadata. Platform defaults are
   * never modified — the write always lands on a tenant row.
   * </p>
   *
   * <p>
   * The response is the **whole re-resolved category**, not an acknowledgement. That is what lets a
   * save propagate to every bound screen without a reload: the caller hands it straight to
   * {@link PlatformConfigurationService.applyCategory}.
   * </p>
   *
   * <p>
   * Unlike the reads, this does **not** swallow failures. A read that fails should fall back to the
   * seed and keep the screen working; a write that fails must reach the administrator, because
   * silently discarding an edit is worse than showing an error.
   * </p>
   */
  saveOverride(
    code: string,
    request: ConfigurationOverrideRequest,
  ): Observable<ResolvedConfigurationCategory | null> {
    return this.put<ConfigurationCategoryDto>(
      `/configuration/categories/${encodeURIComponent(code)}/values`,
      request,
    ).pipe(map(toWriteResult));
  }

  /**
   * Remove this organization's override of a value, restoring the platform default.
   *
   * Restoring a value that is already the default is a legitimate request rather than an error, and
   * the server treats it as one — so this resolves normally and simply returns the unchanged
   * category.
   */
  restoreDefault(
    code: string,
    valueCode: string,
  ): Observable<ResolvedConfigurationCategory | null> {
    return this.delete<ConfigurationCategoryDto>(
      `/configuration/categories/${encodeURIComponent(code)}/values/${encodeURIComponent(valueCode)}`,
    ).pipe(map(toWriteResult));
  }
}

/**
 * Map a write response, **preserving an empty category**.
 *
 * <p>
 * This is where a write differs from a read. On a read, "resolved to nothing" and "could not reach
 * the server" both mean the caller should use its seed, so both collapse to `null`. On a write the
 * two are opposite outcomes: an empty category is the *successful* result of restoring the last
 * override, and the store must be told so it can drop the values it is still holding.
 * </p>
 *
 * <p>
 * Collapsing them here would have left a restored category showing the withdrawn values until the
 * next sign-in — most visibly for the workflow categories, whose platform default lives in code
 * rather than in rows, so "no rows" is the only way to express "use the platform lifecycle".
 * </p>
 */
function toWriteResult(
  category: ConfigurationCategoryDto | null,
): ResolvedConfigurationCategory | null {
  if (!category) return null;

  return {
    code: category.code,
    version: category.version,
    values: category.values.filter((v) => v.isActive && v.code).map(toDescriptor),
  };
}

/**
 * Map a resolved category into descriptors, or `null` when there is nothing usable.
 *
 * `null` for an empty category as well as an unreachable one: both mean "this tenant has no
 * configuration for this domain", and the seed is a better answer than an empty picker.
 */
function toResolved(
  category: ConfigurationCategoryDto | null,
): ResolvedConfigurationCategory | null {
  if (!category || category.values.length === 0) return null;

  const values = category.values.filter((v) => v.isActive && v.code).map(toDescriptor);

  return values.length > 0 ? { code: category.code, version: category.version, values } : null;
}

function toDescriptor(value: ConfigurationValueDto): ConfigurationValueDescriptor {
  const { attributes, compound } = parseMetadata(value.metadata);
  return {
    // The API is the authority on which codes exist, so an unrecognised code is carried through
    // rather than dropped — a value added server-side should appear, not vanish silently.
    id: value.code,
    label: value.name || value.code,
    sortOrder: value.sortOrder,
    attributes,
    compound,
  };
}

/**
 * Split metadata into scalar attributes and structured documents.
 *
 * One parse, two destinations, decided by shape. Scalars go to `attributes`, where every existing
 * consumer reads them and where a nested object would stringify as "[object Object]" in a template.
 * Objects and arrays go to `compound`, for definitions that are genuinely documents — the audit
 * workflow's rules are condition trees, not labelled values.
 */
function parseMetadata(raw: string | null | undefined): {
  attributes: ConfigurationAttributes;
  compound: Readonly<Record<string, unknown>>;
} {
  if (!raw) return { attributes: {}, compound: {} };

  try {
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return { attributes: {}, compound: {} };
    }

    const attributes: Record<string, string | number | boolean> = {};
    const compound: Record<string, unknown> = {};

    for (const [key, v] of Object.entries(parsed as Record<string, unknown>)) {
      if (typeof v === 'string' || typeof v === 'number' || typeof v === 'boolean') {
        attributes[key] = v;
      } else if (v !== null && typeof v === 'object') {
        compound[key] = v;
      }
      // null and undefined are dropped: "configured to nothing" and "not configured" are the same
      // answer to every consumer, and carrying the difference would invite branching on it.
    }

    return { attributes, compound };
  } catch {
    // Malformed metadata must not break a dropdown; the descriptor falls back to nothing set,
    // which every consumer already handles.
    return { attributes: {}, compound: {} };
  }
}
