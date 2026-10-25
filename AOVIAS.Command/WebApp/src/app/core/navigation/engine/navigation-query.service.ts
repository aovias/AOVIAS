import { Injectable, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { sidebarIcon } from '../../layout/sidebar-icons.registry';
import { UserContextService } from '../../services/user-context.service';
import type {
  NavigationBottomItem,
  NavigationBreadcrumbItem,
  NavigationContext,
  NavigationHeaderModule,
  NavigationItem,
  NavigationSidebarSection,
  SidebarViewSection,
} from '../models/navigation.model';
import type { NavigationModule } from '../models/module.model';
import { matchRoute, normalizeNavPath } from '../registry/route-match.util';
import { NavigationEngineService } from './navigation-engine.service';

@Injectable({ providedIn: 'root' })
export class NavigationQueryService {
  private readonly engine = inject(NavigationEngineService);
  private readonly userContext = inject(UserContextService);

  private readonly roles = toSignal(this.userContext.roles$, {
    initialValue: this.userContext.getUserRoles(),
  });

  /** Explicit overrides (tests / feature toggles). */
  private readonly contextOverride = signal<Partial<NavigationContext> | null>(null);

  /** Bumps when roles/features change so computed trees refresh. */
  private readonly version = signal(0);

  private readonly activeContext = computed((): NavigationContext => {
    const override = this.contextOverride();
    const roles = override?.roles ?? this.roles() ?? [];
    return {
      roles,
      permissions: override?.permissions,
      featureOverrides: override?.featureOverrides,
      workspaceId: override?.workspaceId,
    };
  });

  setContext(ctx: NavigationContext): void {
    this.contextOverride.set(ctx);
    this.engine.setContext(ctx);
    this.version.update((v) => v + 1);
  }

  setUserRoles(roles: readonly string[]): void {
    const prev = this.contextOverride();
    this.setContext({
      roles,
      permissions: prev?.permissions,
      featureOverrides: prev?.featureOverrides,
      workspaceId: prev?.workspaceId,
    });
  }

  private snapshot() {
    // Read version + roles so computed invalidates when context changes.
    // Do not write signals here — resolveSurfaces must stay pure for NG0600.
    this.version();
    return this.engine.resolveSurfaces(this.activeContext());
  }

  readonly sidebarSections = computed(() => this.snapshot().sidebarSections);
  readonly headerModules = computed(() => this.snapshot().headerModules);
  readonly bottomItems = computed(() => this.snapshot().bottomItems);

  /** Sidebar sections mapped to legacy view shape (SVG icons). */
  readonly sidebarViewSections = computed((): SidebarViewSection[] =>
    this.sidebarSections().map((s) => this.toSidebarViewSection(s)),
  );

  childrenOf(id: string): readonly NavigationItem[] {
    return this.snapshot().flatVisible.filter((i) => i.parentId === id);
  }

  parentOf(id: string): NavigationItem | null {
    const item = this.findItem(id);
    if (!item?.parentId) return null;
    return this.findItem(item.parentId);
  }

  findItem(id: string): NavigationItem | null {
    return this.engine.allItems().find((i) => i.id === id) ?? null;
  }

  findModule(id: string): NavigationModule | null {
    return this.engine.allModules().find((m) => m.id === id) ?? null;
  }

  findByRoute(url: string): NavigationItem | null {
    const path = normalizeNavPath(url);
    const candidates = this.snapshot().flatVisible.filter((i) => i.route);
    let best: NavigationItem | null = null;
    let bestLen = -1;
    for (const item of candidates) {
      const route = item.route!;
      if (
        matchRoute(path, route, {
          exactActive: item.exactActive,
          subsetActive: item.subsetActive ?? !item.exactActive,
        })
      ) {
        const len = normalizeNavPath(route).length;
        if (len > bestLen) {
          best = item;
          bestLen = len;
        }
      }
    }
    return best;
  }

  titleForUrl(url: string): string | null {
    return this.findByRoute(url)?.title ?? null;
  }

  breadcrumbForUrl(url: string): readonly NavigationBreadcrumbItem[] {
    const item = this.findByRoute(url);
    if (!item || !item.breadcrumb) {
      return Object.freeze([]);
    }
    const trail: NavigationBreadcrumbItem[] = [];
    const mod = this.findModule(item.moduleId);
    if (mod && mod.id !== 'module.workspace') {
      trail.push({ id: mod.id, title: mod.name, route: mod.defaultRoute });
    }
    let cursor: NavigationItem | null = item;
    const stack: NavigationItem[] = [];
    while (cursor) {
      stack.unshift(cursor);
      cursor = cursor.parentId ? this.findItem(cursor.parentId) : null;
    }
    for (const node of stack) {
      trail.push({
        id: node.id,
        title: node.title,
        route: node.route,
      });
    }
    return Object.freeze(trail);
  }

  searchItems(query: string): readonly NavigationItem[] {
    const q = query.trim().toLowerCase();
    const items = this.snapshot().flatVisible.filter(
      (i) => i.searchable && i.surfaces.includes('search'),
    );
    if (!q) return Object.freeze(items.slice());
    return Object.freeze(
      items.filter(
        (i) =>
          i.title.toLowerCase().includes(q) ||
          (i.description?.toLowerCase().includes(q) ?? false) ||
          (i.route?.toLowerCase().includes(q) ?? false),
      ),
    );
  }

  favoritesCandidates(): readonly NavigationItem[] {
    return Object.freeze(this.snapshot().flatVisible.filter((i) => i.favoriteAllowed && !!i.route));
  }

  resolveTitleLabel(route: string): string | null {
    return this.findByRoute(route)?.title ?? null;
  }

  headerModulesRaw(): readonly NavigationHeaderModule[] {
    return this.headerModules();
  }

  bottomItemsRaw(): readonly NavigationBottomItem[] {
    return this.bottomItems();
  }

  private toSidebarViewSection(section: NavigationSidebarSection): SidebarViewSection {
    return {
      id: section.id,
      title: section.title,
      items: section.items
        .filter((i) => !i.parentId)
        .map((item) => ({
          id: item.id,
          label: item.title,
          icon: sidebarIcon(item.icon),
          route: item.route ?? '/',
          primary: item.primary,
          disabled: item.disabled,
          disabledReason: item.disabledReason,
          tooltip: item.tooltip,
          ariaLabel: item.title,
          navBadgeKey: item.badgeKey,
          subsetActive: item.subsetActive,
          requiredRoles: item.roleKeys ? [...item.roleKeys] : undefined,
          children: item.children.map((c) => ({
            id: c.id,
            label: c.title,
            icon: sidebarIcon(c.icon),
            route: c.route ?? '/',
            primary: c.primary,
            disabled: c.disabled,
            disabledReason: c.disabledReason,
            tooltip: c.tooltip,
            ariaLabel: c.title,
            navBadgeKey: c.badgeKey,
            subsetActive: c.subsetActive,
            requiredRoles: c.roleKeys ? [...c.roleKeys] : undefined,
          })),
        })),
    };
  }
}
