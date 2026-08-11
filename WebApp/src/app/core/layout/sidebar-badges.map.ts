import type { BadgeVariant, SidebarMenuItem } from './sidebar-nav.model';
import type { NavigationBadgeKey } from '../navigation/models/badge.model';

export interface NavBadgeState {
  count: number;
  variant: BadgeVariant;
}

export interface ActionCenterBadgeSignals {
  findings: NavBadgeState | null;
  issues: NavBadgeState | null;
  alerts: NavBadgeState | null;
}

/**
 * Resolves badge pill for a nav row from optional config `navBadgeKey`.
 * Prefer NAV_BADGE_RESOLVER / NavigationBadgeStore for new code.
 */
export function resolveNavBadge(
  item: SidebarMenuItem,
  signals: ActionCenterBadgeSignals,
): NavBadgeState | null {
  const key = item.navBadgeKey as NavigationBadgeKey | undefined;
  switch (key) {
    case 'findings':
      return signals.findings;
    case 'issues':
      return signals.issues;
    case 'alerts':
      return signals.alerts;
    default:
      return null;
  }
}
