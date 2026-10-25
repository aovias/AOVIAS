/** Permission key constants (metadata only — evaluation is a future provider). */
export const NavigationPermissionKeys = {
  DashboardView: 'Dashboard.View',
  JourneyView: 'Journey.View',
  EvidenceView: 'Evidence.View',
  TasksView: 'Tasks.View',
  FindingsView: 'Findings.View',
  CapaView: 'Capa.View',
  AuditView: 'Audit.View',
  RisksView: 'Risks.View',
  DocumentationView: 'Documentation.View',
  ReportsView: 'Reports.View',
  SettingsView: 'Settings.View',
} as const;

export type NavigationPermissionKey =
  (typeof NavigationPermissionKeys)[keyof typeof NavigationPermissionKeys];
