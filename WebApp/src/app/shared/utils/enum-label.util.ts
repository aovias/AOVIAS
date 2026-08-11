/**
 * Canonical resolution of API enum values to display labels.
 *
 * **Why this exists.** The platform emits the same enum in several shapes depending on the endpoint:
 * `InternalAudit` (PascalCase), `internalAudit` (camelCase), `INTERNAL_AUDIT` (screaming snake) and,
 * for some contracts, the ordinal as a string (`"0"`). Label maps in feature models were keyed in a
 * single casing, so an exact lookup returned `undefined` and the UI rendered a blank cell — or, on
 * screens with no lookup at all, printed the raw wire value to the user.
 *
 * Every audit-domain enum label must resolve through {@link resolveEnumLabel} so one wire value maps
 * to one display string everywhere. Do not add per-component lookup helpers.
 */

/** Reduce any casing convention to a comparable key: `internalAudit` / `INTERNAL_AUDIT` → `internalaudit`. */
function normalizeEnumKey(value: string): string {
  return value.replace(/[\s_-]/g, '').toLowerCase();
}

/**
 * Build a case- and separator-insensitive lookup from a canonical label map.
 * Pre-computed once per map so per-row rendering stays a single hash lookup.
 */
function buildIndex<T extends string>(labels: Record<T, string>): ReadonlyMap<string, string> {
  const index = new Map<string, string>();
  for (const [key, label] of Object.entries(labels) as [T, string][]) {
    index.set(normalizeEnumKey(key), label);
  }
  return index;
}

const indexCache = new WeakMap<object, ReadonlyMap<string, string>>();

function indexFor<T extends string>(labels: Record<T, string>): ReadonlyMap<string, string> {
  let index = indexCache.get(labels);
  if (!index) {
    index = buildIndex(labels);
    indexCache.set(labels, index);
  }
  return index;
}

/** Split PascalCase / camelCase into spaced words: `draftReport` → `Draft Report`. */
function humanize(value: string): string {
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

/**
 * Resolve an enum value coming off the wire to its display label.
 *
 * Resolution order:
 * 1. Exact match in the label map, ignoring case and separators.
 * 2. `ordinalKeys` positional lookup, for contracts that serialise the ordinal (`"0"`).
 * 3. Humanised form of the raw value — so an enum member added server-side renders as
 *    `Draft Report` rather than a blank cell, and the gap is visible instead of silent.
 * 4. `fallback` (default `'—'`) when the value is null, undefined or empty.
 *
 * @param value    Raw value from the API.
 * @param labels   Canonical label map (any single casing convention).
 * @param ordinals Optional ordinal → member map, for contracts that serialise the numeric value.
 *                 Declared explicitly rather than inferred from array position, because the
 *                 platform's enums are not all zero-based (`FindingRaisedBy` starts at 1).
 * @param fallback Rendered when there is no value at all, or the ordinal is out of range.
 */
export function resolveEnumLabel<T extends string>(
  value: string | number | null | undefined,
  labels: Record<T, string>,
  ordinals?: Readonly<Record<number, T>>,
  fallback = '—',
): string {
  if (value === null || value === undefined) return fallback;

  const raw = String(value).trim();
  if (!raw) return fallback;

  const hit = indexFor(labels).get(normalizeEnumKey(raw));
  if (hit) return hit;

  if (/^\d+$/.test(raw)) {
    const key = ordinals?.[Number(raw)];
    if (key) {
      return labels[key] ?? humanize(key);
    }
    // A number with no member is out-of-range data, not a new enum member. Humanising it would
    // print the bare digit to the user, so fall back instead.
    return fallback;
  }

  return humanize(raw) || fallback;
}

/**
 * Compare an enum value from the wire against a canonical member, ignoring casing and separators.
 *
 * Replaces raw `status === 'Closed'` checks, which silently fail when the API switches casing —
 * the defect behind the permanently-zero Surveillance "Closed" counter.
 */
export function enumEquals(
  value: string | number | null | undefined,
  expected: string,
): boolean {
  if (value === null || value === undefined) return false;
  return normalizeEnumKey(String(value)) === normalizeEnumKey(expected);
}

/** True when `value` matches any of `expected`. See {@link enumEquals}. */
export function enumIn(
  value: string | number | null | undefined,
  expected: readonly string[],
): boolean {
  return expected.some((e) => enumEquals(value, e));
}
