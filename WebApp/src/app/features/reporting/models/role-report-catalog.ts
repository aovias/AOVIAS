/**
 * Role-filtered report catalog (Phase 3 Sprint 6).
 * Filters by RoleExperience packId — Owner/QM/Dept/Consultant/Auditor defaults.
 */

export type RoleExperiencePackId =
  | 'hospital-admin'
  | 'quality-manager'
  | 'department-head'
  | 'consultant'
  | 'auditor';

export interface RoleReportCatalogItem {
  id: string;
  title: string;
  description: string;
  route: string;
  reportCount: number;
  /** Packs that see this report by default. Empty = all packs. */
  packIds: RoleExperiencePackId[];
  color: string;
  icon: string;
}

const ICON_COMPLIANCE =
  '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2"/><path d="M22 4L12 14.01l-3-3" stroke-width="2"/></svg>';
const ICON_AUDIT =
  '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M14 2v6h6M16 13H8m8 4H8m2-8H8" stroke-width="2"/></svg>';
const ICON_TASKS =
  '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M9 11l3 3L22 4" stroke-width="2"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke-width="2"/></svg>';
const ICON_EVIDENCE =
  '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>';
const ICON_READINESS =
  '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" stroke-width="2"/></svg>';
const ICON_CUSTOM =
  '<svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M12 5v14M5 12h14" stroke-width="2"/></svg>';

/** Full catalog — role packs filter via {@link filterReportCatalogByPack}. */
export const ROLE_REPORT_CATALOG: RoleReportCatalogItem[] = [
  {
    id: 'compliance',
    title: 'Compliance Reports',
    description:
      'Monitor compliance performance across chapters with metrics and trend analysis.',
    route: '/app/reports/compliance',
    reportCount: 8,
    packIds: ['hospital-admin', 'quality-manager', 'consultant'],
    color: '#3b82f6',
    icon: ICON_COMPLIANCE,
  },
  {
    id: 'audit',
    title: 'Audit Reports',
    description: 'Analyze audit findings by chapter, department, and severity.',
    route: '/app/reports/audit',
    reportCount: 6,
    packIds: ['hospital-admin', 'quality-manager', 'auditor', 'consultant'],
    color: '#ef4444',
    icon: ICON_AUDIT,
  },
  {
    id: 'tasks',
    title: 'Task Reports',
    description: 'Track completion rates, overdue items, and department workloads.',
    route: '/app/reports/tasks',
    reportCount: 5,
    packIds: ['quality-manager', 'department-head', 'hospital-admin'],
    color: '#0d9488',
    icon: ICON_TASKS,
  },
  {
    id: 'evidence',
    title: 'Evidence Reports',
    description: 'Review documentation coverage, approval status, and upload trends.',
    route: '/app/reports/evidence',
    reportCount: 7,
    packIds: ['quality-manager', 'department-head', 'auditor'],
    color: '#10b981',
    icon: ICON_EVIDENCE,
  },
  {
    id: 'readiness',
    title: 'Readiness Reports',
    description: 'Evaluate accreditation readiness with risk indicators and gaps.',
    route: '/app/reports/readiness',
    reportCount: 4,
    packIds: ['hospital-admin', 'quality-manager', 'consultant'],
    color: '#f59e0b',
    icon: ICON_READINESS,
  },
  {
    id: 'custom',
    title: 'Custom Reports',
    description: 'Build custom reports with flexible dimensions, filters, and export.',
    route: '/app/reports/custom',
    reportCount: 0,
    packIds: ['hospital-admin', 'quality-manager', 'consultant'],
    color: '#2f6bff',
    icon: ICON_CUSTOM,
  },
];

/** Default pack sets documented in 09-reporting-experience.md */
export const DEFAULT_REPORT_PACK_SETS: Record<RoleExperiencePackId, string[]> = {
  'hospital-admin': ['compliance', 'audit', 'tasks', 'readiness', 'custom'],
  'quality-manager': ['compliance', 'audit', 'tasks', 'evidence', 'readiness', 'custom'],
  'department-head': ['tasks', 'evidence'],
  consultant: ['compliance', 'audit', 'readiness', 'custom'],
  auditor: ['audit', 'evidence'],
};

export function normalizePackId(packId?: string | null): RoleExperiencePackId {
  const id = (packId ?? '').trim().toLowerCase();
  if (
    id === 'hospital-admin' ||
    id === 'quality-manager' ||
    id === 'department-head' ||
    id === 'consultant' ||
    id === 'auditor'
  ) {
    return id;
  }
  return 'quality-manager';
}

export function filterReportCatalogByPack(
  packId?: string | null,
  catalog: RoleReportCatalogItem[] = ROLE_REPORT_CATALOG,
): RoleReportCatalogItem[] {
  const pack = normalizePackId(packId);
  return catalog.filter(
    (item) => item.packIds.length === 0 || item.packIds.includes(pack),
  );
}

/** Pure helper for unit tests — resolves dept-head before manager-style packs at the catalog layer. */
export function resolveDeptHeadPackFromRoles(roles: string[]): RoleExperiencePackId | null {
  const combined = roles.map((r) => r.trim());
  if (
    combined.some(
      (r) =>
        /^department[\s_-]?head$/i.test(r) ||
        r === 'DEPARTMENT_HEAD' ||
        r === 'DepartmentHead' ||
        r === 'DeptHead',
    )
  ) {
    return 'department-head';
  }
  return null;
}
