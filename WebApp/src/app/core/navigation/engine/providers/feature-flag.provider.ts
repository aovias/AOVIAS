import { Injectable } from '@angular/core';
import type { NavigationFeatureKey } from '../../models/feature.model';
import type { NavigationContext, NavigationItem } from '../../models/navigation.model';
import type { NavigationModule } from '../../models/module.model';
import { DEFAULT_FEATURE_ENABLED } from '../../registry/feature.registry';
import type { FeatureFlagProvider } from './navigation.tokens';

/**
 * Stub feature flags: registry defaults + context overrides.
 * Items/modules with `enabled: false` are hidden regardless of feature key.
 */
@Injectable({ providedIn: 'root' })
export class DefaultFeatureFlagProvider implements FeatureFlagProvider {
  isFeatureEnabled(key: NavigationFeatureKey | undefined, ctx: NavigationContext): boolean {
    if (!key) return true;
    const override = ctx.featureOverrides?.[key];
    if (typeof override === 'boolean') return override;
    return DEFAULT_FEATURE_ENABLED[key] ?? true;
  }

  isItemEnabled(item: NavigationItem, ctx: NavigationContext): boolean {
    if (!item.enabled) return false;
    return this.isFeatureEnabled(item.featureKey, ctx);
  }

  isModuleEnabled(module: NavigationModule, ctx: NavigationContext): boolean {
    if (!module.enabled) return false;
    return this.isFeatureEnabled(module.featureKey, ctx);
  }
}
