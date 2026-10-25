/**
 * Generic shape of a configuration catalog.
 *
 * <p>
 * **Why generic.** `audit-type.catalog.ts` proved the pattern: a seeded list of descriptors, loaded
 * from the Enterprise Configuration API, read by consumers through
 * {@link PlatformConfigurationService}. Every other Audit business definition — finding statuses,
 * severities, CAPA statuses, risk ratings — has exactly the same shape and differs only in its data.
 * Giving each its own descriptor interface, its own signal and its own loader would reproduce that
 * machinery a dozen times and each copy would drift, which is the problem this wave exists to end.
 * </p>
 *
 * <p>
 * So domain attributes ride in {@link ConfigurationValueDescriptor.attributes} rather than in named
 * fields. That mirrors the server, where `PlatformConfigurationAppService` serves every category
 * from one table with a `Metadata` JSON column — "a new domain needs no schema change and no new
 * code here". The same is now true on the client.
 * </p>
 */

/**
 * Domain attributes for one configurable value, parsed from the category's `metadata` JSON.
 *
 * Deliberately a flat scalar bag: attributes describe *behaviour* consumers branch on
 * (`selectableWhenFiltering`, `lifecycleStep`), and anything needing nesting is a sign the concept
 * belongs in its own category.
 */
export type ConfigurationAttributes = Readonly<Record<string, string | number | boolean>>;

/**
 * One configurable value.
 *
 * `id` is the code as persisted and as sent on the wire; `label` is what the user reads and is what
 * an administrator overrides per organization.
 */
export interface ConfigurationValueDescriptor {
  readonly id: string;
  readonly label: string;
  /** Ordering in pickers and lifecycle bars. Lower first. */
  readonly sortOrder: number;
  readonly attributes: ConfigurationAttributes;
  /**
   * Metadata keys whose values are objects or arrays rather than scalars.
   *
   * <p>
   * Kept separate from {@link attributes} rather than widening it. Every existing consumer reads
   * scalars and would render a nested object as `[object Object]` in a template, so admitting one
   * into the same bag would turn a configuration mistake into a silent display defect.
   * </p>
   *
   * <p>
   * This exists for definitions that are genuinely documents rather than labelled values — the
   * audit workflow's per-stage rules are conditions over facts, which are trees. Consumers must
   * narrow what they read here; it is unvalidated JSON by construction.
   * </p>
   */
  readonly compound: Readonly<Record<string, unknown>>;
}

/**
 * A category and the values that seed it.
 *
 * The seed is not a fallback *branch* — no consumer ever tests "is this configured?". It is the
 * starting content of the same store the API writes into, so the configured and unconfigured paths
 * cannot diverge.
 */
export interface ConfigurationCatalog {
  /** Category code as known to `/configuration/categories/{code}`. */
  readonly code: string;
  /** What this category governs. Surfaces in the adoption report and in Administration. */
  readonly description: string;
  readonly seed: readonly ConfigurationValueDescriptor[];
}

/** `{ value, label }` — the shape every `<select>` in the workspace already binds to. */
export interface ConfigurationOption {
  readonly value: string;
  readonly label: string;
}

/**
 * The code of a configured audit type.
 *
 * <p>
 * Deliberately `string`, and deliberately not a union. The same nine-member union was declared in
 * `auditor-workspace.model.ts` and again in `audit-management-api.service.ts`, and both claimed a
 * closed set — a claim that stopped being true the moment audit types became tenant configuration.
 * A type an administrator adds is valid at runtime and would not typecheck against either union, so
 * the union was not describing the domain; it was contradicting it.
 * </p>
 *
 * <p>
 * The alias is kept rather than inlining `string` so the intent is legible at the call site and so
 * the two former unions have one successor rather than none.
 * </p>
 */
export type AuditTypeCode = string;

/**
 * Build a descriptor with `sortOrder` derived from declaration order.
 *
 * Ordering a catalog is the one thing that is tedious to hand-maintain and easy to get wrong — a
 * duplicated `sortOrder` silently reorders a lifecycle bar. Steps of 10 leave room for an
 * administrator to insert a value between two platform defaults without renumbering.
 */
export function catalogValues(
  entries: readonly (readonly [id: string, label: string, attributes?: ConfigurationAttributes])[],
): readonly ConfigurationValueDescriptor[] {
  return entries.map(([id, label, attributes], index) => ({
    id,
    label,
    sortOrder: (index + 1) * 10,
    attributes: attributes ?? {},
    compound: {},
  }));
}

/**
 * Reduce casing and separators so `internalAudit`, `INTERNAL_AUDIT` and `InternalAudit` match.
 *
 * The API emits camelCase while catalogs are keyed PascalCase; the mismatch blanked the Type column
 * across two screens (DEF-004, DEF-013). Every lookup goes through here so it cannot recur.
 */
export function normaliseConfigurationId(value: string): string {
  return value.replace(/[\s_-]/g, '').toLowerCase();
}

/** `draftReport` → `Draft Report`, so a value the platform does not know is still readable. */
export function humaniseConfigurationId(value: string): string {
  const spaced = value
    .replace(/[_-]+/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .trim();
  if (!spaced) return '';
  return spaced
    .split(/\s+/)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ');
}
