import { Injectable } from '@angular/core';
import type {
  NavigationBottomItem,
  NavigationContext,
  NavigationHeaderModule,
  NavigationItem,
  NavigationSidebarSection,
} from '../models/navigation.model';

export interface NavigationSurfaceCache {
  readonly contextKey: string;
  readonly sidebarSections: readonly NavigationSidebarSection[];
  readonly headerModules: readonly NavigationHeaderModule[];
  readonly bottomItems: readonly NavigationBottomItem[];
  readonly flatVisible: readonly NavigationItem[];
}

/**
 * In-memory surface cache. Uses a plain field (not a signal) so resolve can
 * run inside Query computed() without NG0600 write errors.
 */
@Injectable({ providedIn: 'root' })
export class NavigationCacheService {
  private cache: NavigationSurfaceCache | null = null;

  static contextKey(ctx: NavigationContext): string {
    const roles = [...ctx.roles]
      .map((r) => r.trim().toUpperCase())
      .sort()
      .join(',');
    const perms = [...(ctx.permissions ?? [])]
      .map((p) => p.trim())
      .sort()
      .join(',');
    const features = ctx.featureOverrides
      ? Object.entries(ctx.featureOverrides)
          .sort(([a], [b]) => a.localeCompare(b))
          .map(([k, v]) => `${k}:${v}`)
          .join(',')
      : '';
    return `${roles}|${perms}|${features}|${ctx.workspaceId ?? ''}`;
  }

  get(ctx: NavigationContext): NavigationSurfaceCache | null {
    const current = this.cache;
    if (!current) return null;
    return current.contextKey === NavigationCacheService.contextKey(ctx) ? current : null;
  }

  set(ctx: NavigationContext, value: Omit<NavigationSurfaceCache, 'contextKey'>): void {
    this.cache = {
      contextKey: NavigationCacheService.contextKey(ctx),
      ...value,
    };
  }

  invalidate(): void {
    this.cache = null;
  }
}
