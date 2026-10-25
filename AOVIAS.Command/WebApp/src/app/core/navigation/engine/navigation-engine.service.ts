import { Injectable, inject, signal } from '@angular/core';
import type { NavigationFeatureKey } from '../models/feature.model';
import type {
  NavigationBottomItem,
  NavigationContext,
  NavigationExtension,
  NavigationHeaderModule,
  NavigationItem,
  NavigationSidebarSection,
  NavigationSurface,
  NavigationTreeNode,
} from '../models/navigation.model';
import type { NavigationModule } from '../models/module.model';
import { CATEGORY_REGISTRY } from '../registry/category.registry';
import { MENU_REGISTRY } from '../registry/menu.registry';
import { MODULE_REGISTRY } from '../registry/module.registry';
import { NavigationCacheService } from './navigation-cache.service';
import {
  NAV_FEATURE_FLAGS,
  NAV_PERMISSION_VISIBILITY,
  NAV_ROLE_VISIBILITY,
} from './providers/navigation.tokens';

@Injectable({ providedIn: 'root' })
export class NavigationEngineService {
  private readonly cache = inject(NavigationCacheService);
  private readonly roles = inject(NAV_ROLE_VISIBILITY);
  private readonly permissions = inject(NAV_PERMISSION_VISIBILITY);
  private readonly features = inject(NAV_FEATURE_FLAGS);

  private readonly extensionItems = signal<readonly NavigationItem[]>([]);
  private readonly extensionModules = signal<readonly NavigationModule[]>([]);

  private readonly context = signal<NavigationContext>({ roles: [] });

  setContext(ctx: NavigationContext): void {
    const prev = this.context();
    const next: NavigationContext = {
      roles: ctx.roles,
      permissions: ctx.permissions,
      featureOverrides: ctx.featureOverrides,
      workspaceId: ctx.workspaceId,
    };
    if (NavigationCacheService.contextKey(prev) === NavigationCacheService.contextKey(next)) {
      return;
    }
    this.context.set(next);
    this.cache.invalidate();
  }

  getContext(): NavigationContext {
    return this.context();
  }

  /** In-memory merge for future marketplace / plugin modules. */
  registerNavigationExtension(extension: NavigationExtension): void {
    if (extension.items?.length) {
      this.extensionItems.update((cur) => Object.freeze([...cur, ...extension.items!]));
    }
    if (extension.modules?.length) {
      this.extensionModules.update((cur) => Object.freeze([...cur, ...extension.modules!]));
    }
    this.cache.invalidate();
  }

  allItems(): readonly NavigationItem[] {
    const ext = this.extensionItems();
    return ext.length ? Object.freeze([...MENU_REGISTRY, ...ext]) : MENU_REGISTRY;
  }

  allModules(): readonly NavigationModule[] {
    const ext = this.extensionModules();
    return ext.length ? Object.freeze([...MODULE_REGISTRY, ...ext]) : MODULE_REGISTRY;
  }

  resolveSurfaces(ctx?: NavigationContext) {
    const context = ctx ?? this.context();
    const hit = this.cache.get(context);
    if (hit) return hit;

    const modules = this.allModules()
      .filter((m) => this.pipelineModule(m, context))
      .slice()
      .sort((a, b) => a.order - b.order);

    const visibleModuleIds = new Set(modules.map((m) => m.id));
    const flat = this.allItems()
      .filter((item) => item.visible)
      .filter((item) => this.features.isItemEnabled(item, context))
      .filter((item) => this.roles.isItemVisible(item, context))
      .filter((item) => this.permissions.isItemVisible(item, context))
      .filter(
        (item) => visibleModuleIds.has(item.moduleId) || item.moduleId === 'module.workspace',
      );

    const sidebarSections = this.buildSidebar(flat);
    const headerModules = this.buildHeader(modules, flat);
    const bottomItems = this.buildBottom(flat);

    const snapshot = {
      sidebarSections,
      headerModules,
      bottomItems,
      flatVisible: Object.freeze(flat.slice().sort((a, b) => a.order - b.order)),
    };
    this.cache.set(context, snapshot);
    return { contextKey: NavigationCacheService.contextKey(context), ...snapshot };
  }

  private pipelineModule(module: NavigationModule, ctx: NavigationContext): boolean {
    if (!this.features.isModuleEnabled(module, ctx)) return false;
    if (!this.roles.isModuleVisible(module, ctx)) return false;
    return true;
  }

  private forSurface(
    items: readonly NavigationItem[],
    surface: NavigationSurface,
  ): NavigationItem[] {
    return items
      .filter((i) => i.surfaces.includes(surface))
      .slice()
      .sort((a, b) => a.order - b.order);
  }

  private buildSidebar(flat: readonly NavigationItem[]): readonly NavigationSidebarSection[] {
    const items = this.forSurface(flat, 'sidebar');
    const categories = CATEGORY_REGISTRY.filter((c) => c.surface === 'sidebar')
      .slice()
      .sort((a, b) => a.order - b.order);

    return Object.freeze(
      categories
        .map((cat) => {
          const catItems = items
            .filter((i) => i.categoryId === cat.id && !i.parentId)
            .map((i) => this.toNode(i, items));
          return Object.freeze({
            id: cat.id,
            title: cat.title,
            order: cat.order,
            items: Object.freeze(catItems),
          });
        })
        .filter((s) => s.items.length > 0),
    );
  }

  private buildHeader(
    modules: readonly NavigationModule[],
    flat: readonly NavigationItem[],
  ): readonly NavigationHeaderModule[] {
    const headerItems = this.forSurface(flat, 'header');
    return Object.freeze(
      modules
        .filter((m) => m.id !== 'module.workspace')
        .map((mod) => {
          const items = headerItems.filter((i) => i.moduleId === mod.id);
          if (!items.length) return null;
          return Object.freeze({
            id: mod.id,
            title: mod.name,
            icon: mod.icon,
            route: mod.defaultRoute,
            order: mod.order,
            disabled: items.every((i) => i.disabled),
            items: Object.freeze(items),
          } satisfies NavigationHeaderModule);
        })
        .filter((m): m is NavigationHeaderModule => m != null),
    );
  }

  private buildBottom(flat: readonly NavigationItem[]): readonly NavigationBottomItem[] {
    return Object.freeze(
      this.forSurface(flat, 'bottom').map((i) =>
        Object.freeze({
          id: i.id,
          title: i.title,
          route: i.route ?? '/',
          icon: i.icon,
          order: i.order,
        }),
      ),
    );
  }

  private toNode(item: NavigationItem, pool: readonly NavigationItem[]): NavigationTreeNode {
    const children = pool
      .filter((c) => c.parentId === item.id)
      .slice()
      .sort((a, b) => a.order - b.order)
      .map((c) => this.toNode(c, pool));
    return Object.freeze({ ...item, children: Object.freeze(children) });
  }

  /** Test helper: hide a feature without UI changes. */
  withFeatureOverride(key: NavigationFeatureKey, enabled: boolean): void {
    const ctx = this.context();
    this.setContext({
      ...ctx,
      featureOverrides: { ...ctx.featureOverrides, [key]: enabled },
    });
  }
}
