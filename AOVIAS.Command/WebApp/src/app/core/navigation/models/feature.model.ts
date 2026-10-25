/** Feature flag keys (NAV-3). Prefer constants over string literals. */
export const NavigationFeatureKeys = {
  ComplianceDashboard: 'ComplianceDashboard',
  ComplianceGraph: 'ComplianceGraph',
  QualityIntelligence: 'QualityIntelligence',
  PatientSafety: 'PatientSafety',
  Risks: 'Risks',
  Documentation: 'Documentation',
  Reports: 'Reports',
  Audit: 'Audit',
  Knowledge: 'Knowledge',
  Settings: 'Settings',
  AiComplianceCopilot: 'AIComplianceCopilot',
} as const;

export type NavigationFeatureKey =
  (typeof NavigationFeatureKeys)[keyof typeof NavigationFeatureKeys];

export interface NavigationFeature {
  readonly key: NavigationFeatureKey;
  readonly enabled: boolean;
  readonly beta?: boolean;
  readonly preview?: boolean;
  readonly experimental?: boolean;
}
