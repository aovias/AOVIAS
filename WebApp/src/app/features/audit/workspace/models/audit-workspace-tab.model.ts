import type { PageTabItem } from '../../../../shared/ui/navigation/page-tabs/page-tabs.component';

/**
 * Audit Workspace capability tabs.
 *
 * Tabs represent **business capabilities**, not pages. Each hosts existing,
 * unmodified page components through the workspace router-outlet.
 *
 * Phase 2 made every capability path canonical and capability-named. Legacy
 * paths (`assessment`, `corrective-actions`, `simulation`, `surveillance`,
 * `internal`, `final`, `calendar`) and the whole `/app/auditor/**` tree now
 * redirect here, so there is exactly one URL per capability and no duplicate
 * entry point.
 */
export const AUDIT_WORKSPACE_BASE = '/app/audit';

/** Stable ids — used for state persistence and tab resolution from the URL. */
export type AuditWorkspaceTabId =
  | 'overview'
  | 'planning'
  | 'execution'
  | 'evidence'
  | 'findings'
  | 'capa'
  | 'reports'
  | 'intelligence'
  | 'administration';

export interface AuditWorkspaceTab extends PageTabItem {
  id: AuditWorkspaceTabId;
  /**
   * Legacy first-segment aliases that still resolve to this capability, so the
   * correct tab is reported while a redirect is in flight.
   */
  matches?: readonly string[];
}

export const AUDIT_WORKSPACE_TABS: readonly AuditWorkspaceTab[] = [
  {
    id: 'overview',
    label: 'Overview',
    route: `${AUDIT_WORKSPACE_BASE}/overview`,
    matches: ['internal'],
  },
  {
    id: 'planning',
    label: 'Planning',
    route: `${AUDIT_WORKSPACE_BASE}/planning`,
    matches: ['calendar'],
  },
  {
    id: 'execution',
    label: 'Execution',
    route: `${AUDIT_WORKSPACE_BASE}/execution`,
    matches: ['simulation', 'surveillance'],
  },
  {
    id: 'evidence',
    label: 'Evidence',
    route: `${AUDIT_WORKSPACE_BASE}/evidence`,
    matches: ['assessment'],
  },
  {
    id: 'findings',
    label: 'Findings',
    route: `${AUDIT_WORKSPACE_BASE}/findings`,
  },
  {
    id: 'capa',
    label: 'CAPA',
    route: `${AUDIT_WORKSPACE_BASE}/capa`,
    matches: ['corrective-actions'],
  },
  {
    id: 'reports',
    label: 'Reports',
    route: `${AUDIT_WORKSPACE_BASE}/reports`,
    matches: ['final'],
  },
  {
    id: 'intelligence',
    label: 'Intelligence',
    route: `${AUDIT_WORKSPACE_BASE}/intelligence`,
  },
  {
    id: 'administration',
    label: 'Administration',
    route: `${AUDIT_WORKSPACE_BASE}/administration`,
  },
];

/** First URL segment under /app/audit → owning capability tab. */
export function resolveAuditTabFromUrl(url: string): AuditWorkspaceTabId {
  const path = url.split('?')[0].split('#')[0];
  const marker = `${AUDIT_WORKSPACE_BASE}/`;
  const idx = path.indexOf(marker);
  if (idx < 0) return 'overview';

  const segment = path.slice(idx + marker.length).split('/')[0];
  if (!segment) return 'overview';

  const tab = AUDIT_WORKSPACE_TABS.find(
    (t) => t.route?.toString().endsWith(`/${segment}`) || t.matches?.includes(segment),
  );
  return tab?.id ?? 'overview';
}
