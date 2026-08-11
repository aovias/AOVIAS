import { makeEnvironmentProviders, type EnvironmentProviders } from '@angular/core';
import {
  AlertsBadgeProvider,
  CompositeBadgeResolver,
  FindingsBadgeProvider,
  IssuesBadgeProvider,
} from './providers/badge.provider';
import { DefaultFeatureFlagProvider } from './providers/feature-flag.provider';
import {
  NAV_BADGE_PROVIDERS,
  NAV_BADGE_RESOLVER,
  NAV_FEATURE_FLAGS,
  NAV_PERMISSION_VISIBILITY,
  NAV_ROLE_VISIBILITY,
} from './providers/navigation.tokens';
import { NoOpPermissionVisibilityProvider } from './providers/permission-visibility.provider';
import { DefaultRoleVisibilityProvider } from './providers/role-visibility.provider';

/** Registers NAV-3 visibility / badge provider tokens. */
export function provideNavigationFramework(): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: NAV_ROLE_VISIBILITY, useExisting: DefaultRoleVisibilityProvider },
    { provide: NAV_PERMISSION_VISIBILITY, useExisting: NoOpPermissionVisibilityProvider },
    { provide: NAV_FEATURE_FLAGS, useExisting: DefaultFeatureFlagProvider },
    { provide: NAV_BADGE_RESOLVER, useExisting: CompositeBadgeResolver },
    {
      provide: NAV_BADGE_PROVIDERS,
      useFactory: (
        findings: FindingsBadgeProvider,
        issues: IssuesBadgeProvider,
        alerts: AlertsBadgeProvider,
      ) => [findings, issues, alerts],
      deps: [FindingsBadgeProvider, IssuesBadgeProvider, AlertsBadgeProvider],
    },
  ]);
}
