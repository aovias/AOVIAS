/**
 * Wire contract helper for the findings API.
 *
 * <p>
 * This file used to also declare the finding severities, statuses, departments and sources as
 * `as const` tuples with matching union types, plus the request/response interfaces that used them.
 * Nothing imported any of it — the Findings Workbench had long since moved to
 * `findings-workspace.model.ts` — so it was a fourth copy of definitions that now live in the
 * Configuration Engine, kept alive only by being exported. It is gone.
 * </p>
 *
 * <p>
 * What remains is genuinely not configuration: ComplianceService serialises these enums with
 * `JsonStringEnumConverter`, which emits camelCase, while the platform's configured codes are
 * PascalCase. That casing rule is a property of the transport, not a business decision an
 * administrator makes, so it stays in code.
 * </p>
 */

/** camelCase wire value for POST /findings enum fields (JsonStringEnumConverter). */
export function toFindingWireEnum(value: string): string {
  if (!value) return value;
  return value.charAt(0).toLowerCase() + value.slice(1);
}
