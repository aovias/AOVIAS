/**
 * Central SVG snippets for shell sidebar + bottom nav.
 * Size is controlled by `.menu-icon` / `.app-bottom-nav__icon` + `--icon-size-nav` (not inline px).
 */

export type SidebarSemanticIconId =
  | 'overview'
  | 'analytics'
  | 'journey'
  | 'standardsChapters'
  | 'evidence'
  | 'tasks'
  | 'findings'
  | 'issuesRisks'
  | 'alertsNotifications'
  | 'reports'
  | 'internalAudit'
  | 'finalAudit'
  | 'surveillance'
  | 'policies'
  | 'documents'
  | 'knowledgeGraph'
  | 'complianceControlCenter'
  | 'complianceMaturity'
  | 'copilot';

const SIDEBAR_ICONS: Record<SidebarSemanticIconId, string> = {
  overview:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M9 22V12h6v10" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  analytics:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M18 20V10M12 20V4M6 20v-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  journey:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  standardsChapters:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M4 19.5A2.5 2.5 0 016.5 17H20" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  evidence:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  tasks:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><rect x="3" y="5" width="6" height="6" rx="1" stroke-width="2"/><rect x="3" y="13" width="6" height="6" rx="1" stroke-width="2"/><path d="M13 6h8m-8 12h8m-8-6h8" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  findings:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/><path d="M12 9v4m0 4h.01" stroke-width="2" stroke-linecap="round"/></svg>',
  /** Shield + alert — Issues & Risks */
  issuesRisks:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M12 8v4m0 4h.01" stroke-width="2" stroke-linecap="round"/></svg>',
  alertsNotifications:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  reports:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M9 17H7A5 5 0 017 7h2m6 0h2a5 5 0 110 10h-2m-6-5h8" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><rect x="6" y="3" width="4" height="18" rx="1" stroke-width="2"/></svg>',
  internalAudit:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><circle cx="11" cy="11" r="8" stroke-width="2"/><path d="M21 21l-4.35-4.35" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  finalAudit:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M9 11l3 3L22 4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  surveillance:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" stroke-width="2"/><circle cx="12" cy="12" r="3" stroke-width="2"/></svg>',
  policies:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M14 2v6h6M16 13H8M16 17H8M10 9H8" stroke-width="2" stroke-linecap="round"/></svg>',
  documents:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2" stroke-linecap="round"/></svg>',
  knowledgeGraph:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><circle cx="18" cy="5" r="3" stroke-width="2"/><circle cx="6" cy="12" r="3" stroke-width="2"/><circle cx="18" cy="19" r="3" stroke-width="2"/><path d="M8.59 13.51l6.83 3.98m-.01-10.98l-6.82 3.98" stroke-width="2"/></svg>',
  complianceControlCenter:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><circle cx="12" cy="12" r="3" stroke-width="2"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83 0 2 2 0 010-2.83l.06-.06a1.65 1.65 0 00.33-1.82 1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 010-2.83 2 2 0 012.83 0l.06.06a1.65 1.65 0 001.82.33H9a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 0 2 2 0 010 2.83l-.06.06a1.65 1.65 0 00-.33 1.82V9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  /** Four-stage progress dots — Compliance Maturity Journey */
  complianceMaturity:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><circle cx="4" cy="12" r="2" stroke-width="2"/><circle cx="10" cy="12" r="2" stroke-width="2"/><circle cx="16" cy="12" r="2" stroke-width="2"/><circle cx="22" cy="12" r="2" stroke-width="2"/><path d="M6 12h2m4 0h2m4 0h2" stroke-width="1.5" stroke-linecap="round"/><path d="M4 7l2 5m4-5l2 5m4-5l2 5" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" opacity="0.4"/></svg>',
  copilot:
    '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" aria-hidden="true"><path d="M12 3a7 7 0 017 7v2a5 5 0 015 5v1H3v-1a5 5 0 015-5V10a7 7 0 017-7z" stroke-width="2"/><path d="M9 22h6M10 18h4" stroke-width="2" stroke-linecap="round"/></svg>',
};

export function sidebarIcon(id: SidebarSemanticIconId): string {
  return SIDEBAR_ICONS[id];
}
