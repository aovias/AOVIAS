export const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/** Returns null when the value is missing or the all-zero GUID sentinel. */
export function normalizeGuid(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();
  if (!trimmed || trimmed === EMPTY_GUID) {
    return null;
  }

  return trimmed;
}

export function isEmptyGuid(value: string | null | undefined): boolean {
  return normalizeGuid(value) === null && !!value?.trim();
}
