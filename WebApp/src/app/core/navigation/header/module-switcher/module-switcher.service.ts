import { Injectable, computed, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import {
  isComplianceGraphShellNavPath,
  isDocumentationShellNavPath,
  isPatientSafetyShellNavPath,
  isQualityIntelligenceShellNavPath,
  isRisksShellNavPath,
  type HeaderNavGroup,
  type HeaderNavItem,
} from '../../sidebar/sidebar.models';
import { WorkspacePreferencesService } from '../../personalization/workspace-preferences.service';
import { HeaderProvider } from '../../surfaces/header.provider';
import type { ModuleEntry } from './module-switcher.models';

@Injectable({ providedIn: 'root' })
export class ModuleSwitcherService {
  private readonly router = inject(Router);
  private readonly headerProvider = inject(HeaderProvider);
  private readonly prefs = inject(WorkspacePreferencesService);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly modules = computed(() => {
    const entries = this.toModuleEntries(this.headerProvider.headerNavGroups());
    const order = this.prefs.prefs().moduleOrder;
    if (!order.length) return entries;
    const rank = new Map(order.map((id, i) => [id, i]));
    return [...entries].sort((a, b) => {
      const ra = rank.get(a.id) ?? rank.get(`module.${a.id.replace(/^header-/, '')}`) ?? 999;
      const rb = rank.get(b.id) ?? rank.get(`module.${b.id.replace(/^header-/, '')}`) ?? 999;
      return ra - rb;
    });
  });

  readonly activeModule = computed(() => {
    const url = this.url() ?? this.router.url;
    const list = this.modules();
    return list.find((m) => this.isModuleActive(m, url)) ?? null;
  });

  toModuleEntries(groups: HeaderNavGroup[]): ModuleEntry[] {
    return groups.map((g) => ({
      id: g.id,
      title: g.title,
      icon: g.sectionIcon,
      route: this.primaryRoute(g),
      category: 'Modules',
      items: g.items,
      disabled: g.items.every((i) => i.disabled),
    }));
  }

  primaryRoute(group: HeaderNavGroup): string {
    const first = group.items.find((i) => !i.disabled);
    return first?.route ?? group.items[0]?.route ?? '/app/dashboard';
  }

  filterModules(modules: ModuleEntry[], query: string): ModuleEntry[] {
    const q = query.trim().toLowerCase();
    if (!q) return modules;
    return modules.filter((m) => {
      if (m.title.toLowerCase().includes(q)) return true;
      return m.items.some((i) => i.label.toLowerCase().includes(q));
    });
  }

  isModuleActive(module: ModuleEntry, url: string): boolean {
    return module.items.some((it) => !it.disabled && this.isItemActive(it, url));
  }

  isItemActive(item: HeaderNavItem, url: string): boolean {
    if (item.exactActive) {
      const path = url.split(/[?#]/)[0] ?? '';
      const a = path.replace(/\/+$/, '') || '/';
      const b = (item.route || '/').replace(/\/+$/, '') || '/';
      return a === b;
    }
    if (item.label === 'Documentation Workspace' && item.route === '/app/documentation') {
      return isDocumentationShellNavPath(url);
    }
    if (item.label === 'Patient Safety Workspace' && item.route === '/app/patient-safety') {
      return isPatientSafetyShellNavPath(url);
    }
    if (item.label === 'Enterprise Risk Register' && item.route === '/app/risks/register') {
      return isRisksShellNavPath(url);
    }
    if (
      item.label === 'Quality Intelligence Workspace' &&
      item.route === '/app/quality-intelligence'
    ) {
      return isQualityIntelligenceShellNavPath(url);
    }
    if (item.label === 'Compliance Graph Workspace' && item.route === '/app/compliance-graph') {
      return isComplianceGraphShellNavPath(url);
    }
    return this.router.isActive(item.route, {
      paths: 'subset',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored',
    });
  }

  navigateToModule(module: ModuleEntry): void {
    if (module.disabled || !module.route) return;
    void this.router.navigateByUrl(module.route);
  }
}
