import { InjectionToken } from '@angular/core';
import type {
  NavigationBadgeKey,
  NavigationBadgeProvider,
  NavigationBadgeSnapshot,
} from '../../models/badge.model';
import type { NavigationFeatureKey } from '../../models/feature.model';
import type { NavigationContext, NavigationItem } from '../../models/navigation.model';
import type { NavigationModule } from '../../models/module.model';

export interface RoleVisibilityProvider {
  isItemVisible(item: NavigationItem, ctx: NavigationContext): boolean;
  isModuleVisible(module: NavigationModule, ctx: NavigationContext): boolean;
}

export interface PermissionVisibilityProvider {
  isItemVisible(item: NavigationItem, ctx: NavigationContext): boolean;
}

export interface FeatureFlagProvider {
  isFeatureEnabled(key: NavigationFeatureKey | undefined, ctx: NavigationContext): boolean;
  isItemEnabled(item: NavigationItem, ctx: NavigationContext): boolean;
  isModuleEnabled(module: NavigationModule, ctx: NavigationContext): boolean;
}

export interface BadgeResolver {
  resolve(key: NavigationBadgeKey): NavigationBadgeSnapshot | null;
}

export const NAV_ROLE_VISIBILITY = new InjectionToken<RoleVisibilityProvider>(
  'NAV_ROLE_VISIBILITY',
);
export const NAV_PERMISSION_VISIBILITY = new InjectionToken<PermissionVisibilityProvider>(
  'NAV_PERMISSION_VISIBILITY',
);
export const NAV_FEATURE_FLAGS = new InjectionToken<FeatureFlagProvider>('NAV_FEATURE_FLAGS');
export const NAV_BADGE_PROVIDERS = new InjectionToken<readonly NavigationBadgeProvider[]>(
  'NAV_BADGE_PROVIDERS',
);
export const NAV_BADGE_RESOLVER = new InjectionToken<BadgeResolver>('NAV_BADGE_RESOLVER');
