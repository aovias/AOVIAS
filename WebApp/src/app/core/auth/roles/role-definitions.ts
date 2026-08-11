/**
 * Canonical application role identifiers — single source of truth for APIs and client state.
 * UI must use {@link ROLE_LABELS} or {@link RoleRegistryService#labelOf} for display only.
 */
export const ROLE_IDS = [
  'USER',
  'ORG_ADMIN',
  'GROUP_ADMIN',
  'SUPER_ADMIN',
  'REGULATORY_ADMIN',
  'COMPLIANCE_VIEWER',
  'AUTHORITY',
  'REGULATOR',
  'MANAGER',
  'DEPARTMENT_HEAD',
  'EXTERNAL_AUDITOR',
  'COMMERCIAL_ADMIN',
  'CSM',
  'SUPPORT_ENGINEER',
  'LICENSE_ADMIN',
  'SALES_OPS',
  'COMPLIANCE_OPS',
  'COMMERCIAL_AUDITOR',
] as const;

export type RoleId = (typeof ROLE_IDS)[number];

export const UNKNOWN_ROLE_LABEL = 'Unknown Role';

/** Human-readable labels for UI only — never send these in API bodies. */
export const ROLE_LABELS: Record<RoleId, string> = {
  USER: 'User',
  ORG_ADMIN: 'Organization Admin',
  GROUP_ADMIN: 'Group Admin',
  SUPER_ADMIN: 'Super Admin',
  REGULATORY_ADMIN: 'Regulatory Admin',
  COMPLIANCE_VIEWER: 'Compliance Viewer',
  AUTHORITY: 'Authority',
  REGULATOR: 'Regulator',
  MANAGER: 'Manager',
  DEPARTMENT_HEAD: 'Department Head',
  EXTERNAL_AUDITOR: 'External Auditor',
  COMMERCIAL_ADMIN: 'Commercial Administrator',
  CSM: 'Customer Success Manager',
  SUPPORT_ENGINEER: 'Support Engineer',
  LICENSE_ADMIN: 'License Administrator',
  SALES_OPS: 'Sales Operations',
  COMPLIANCE_OPS: 'Compliance Operations',
  COMMERCIAL_AUDITOR: 'Commercial Auditor',
};

/**
 * Maps legacy onboarding / display strings to canonical {@link RoleId} for drafts and migrations.
 */
export const LEGACY_ROLE_ALIASES: Record<string, RoleId> = {
  'Quality Manager': 'MANAGER',
  'Compliance Officer': 'COMPLIANCE_VIEWER',
  'Department Head': 'DEPARTMENT_HEAD',
  DepartmentHead: 'DEPARTMENT_HEAD',
  DeptHead: 'DEPARTMENT_HEAD',
  Auditor: 'REGULATOR',
  'External Auditor': 'EXTERNAL_AUDITOR',
  Administrator: 'ORG_ADMIN',
};

export function isRoleId(value: string): value is RoleId {
  return (ROLE_IDS as readonly string[]).includes(value);
}

/**
 * Resolves a raw string to a known {@link RoleId}, including legacy aliases.
 * Returns `null` if empty or unrecognized (caller may keep raw for diagnostics).
 */
export function normalizeRoleId(raw: string | null | undefined): RoleId | null {
  if (raw == null) return null;
  const t = raw.trim();
  if (!t) return null;
  if (isRoleId(t)) return t;
  return LEGACY_ROLE_ALIASES[t] ?? null;
}

/** Type-safe tuple for `roleGuard` route configs. */
export function asRoleIds<T extends readonly RoleId[]>(ids: T): T {
  return ids;
}

/** AOVIAS staff roles for the Commercial Administration Console (excludes ORG_ADMIN). */
export const COMMERCIAL_ADMIN_ROLE_IDS = asRoleIds([
  'SUPER_ADMIN',
  'COMMERCIAL_ADMIN',
  'CSM',
  'SUPPORT_ENGINEER',
  'LICENSE_ADMIN',
  'SALES_OPS',
  'COMPLIANCE_OPS',
  'COMMERCIAL_AUDITOR',
] as const);
