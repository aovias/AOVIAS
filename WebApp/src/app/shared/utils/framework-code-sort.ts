import { FrameworkLookup } from '../models/lookup.models';

/**
 * Parses the numeric segment from framework codes like `CP00000001` (prefix `CP` + digits).
 * Leading zeros are handled via integer parsing, not string order.
 */
export function frameworkCodeNumericSortKey(code: string | undefined | null): number | null {
  const s = code?.trim();
  if (!s) return null;
  const m = /^CP(\d+)$/i.exec(s);
  if (!m) return null;
  const n = Number.parseInt(m[1], 10);
  return Number.isFinite(n) ? n : null;
}

export function compareFrameworkCodeOrder(
  a: { code: string; name: string },
  b: { code: string; name: string },
): number {
  const na = frameworkCodeNumericSortKey(a.code);
  const nb = frameworkCodeNumericSortKey(b.code);
  const aHas = na !== null;
  const bHas = nb !== null;
  if (aHas && bHas && na !== nb) return na! - nb!;
  if (aHas && !bHas) return -1;
  if (!aHas && bHas) return 1;
  if (aHas && bHas && na === nb) {
    return a.name.localeCompare(b.name, undefined, { sensitivity: 'base' });
  }
  return a.name.localeCompare(b.name, undefined, { sensitivity: 'base' });
}

/** Rows that expose `code` and `name` (e.g. `FrameworkLookup`, hospital selector items). */
export function sortByCpFrameworkCode<T extends { code: string; name: string }>(rows: readonly T[]): T[] {
  return [...rows].sort(compareFrameworkCodeOrder);
}

export function sortFrameworkLookupsByCode(rows: readonly FrameworkLookup[]): FrameworkLookup[] {
  return sortByCpFrameworkCode(rows);
}

/** For rows that use `frameworkCode` / `frameworkName` (snapshots, journeys). */
export function sortByCpFrameworkCodeKeys<T>(
  rows: readonly T[],
  getCode: (row: T) => string,
  getName: (row: T) => string,
): T[] {
  return [...rows].sort((a, b) =>
    compareFrameworkCodeOrder(
      { code: getCode(a), name: getName(a) },
      { code: getCode(b), name: getName(b) },
    ),
  );
}
