export type NavigationBadgeVariant = 'neutral' | 'warning' | 'critical';

export const NavigationBadgeKeys = {
  Findings: 'findings',
  Issues: 'issues',
  Alerts: 'alerts',
} as const;

export type NavigationBadgeKey = (typeof NavigationBadgeKeys)[keyof typeof NavigationBadgeKeys];

export interface NavigationBadgeSnapshot {
  readonly count: number;
  readonly variant: NavigationBadgeVariant;
}

export interface NavigationBadgeProvider {
  readonly key: NavigationBadgeKey;
  resolve(): NavigationBadgeSnapshot | null;
}
