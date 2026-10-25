import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SessionService } from '../../auth/session.service';
import { NavigationHistoryService } from '../engine/navigation-history.service';
import { NavigationQueryService } from '../engine/navigation-query.service';
import type { SearchIndexEntry } from '../search/search-sources';
import { sidebarIcon } from '../../layout/sidebar-icons.registry';

export interface UsageStat {
  readonly id: string;
  readonly route?: string;
  readonly moduleId?: string;
  readonly count: number;
  readonly lastAccess: number;
}

const USAGE_KEY = 'praman.nav.usage.v1';

@Injectable({ providedIn: 'root' })
export class PersonalizationService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly session = inject(SessionService);
  private readonly history = inject(NavigationHistoryService);
  private readonly query = inject(NavigationQueryService);

  private readonly usageSig = signal<Record<string, UsageStat>>(this.load());

  readonly usage = this.usageSig.asReadonly();

  trackUsage(entry: { id?: string; route: string; moduleId?: string }): void {
    const id = entry.id ?? entry.route;
    const prev = this.usageSig()[id];
    const next: UsageStat = {
      id,
      route: entry.route,
      moduleId: entry.moduleId ?? prev?.moduleId,
      count: (prev?.count ?? 0) + 1,
      lastAccess: Date.now(),
    };
    const map = { ...this.usageSig(), [id]: next };
    this.usageSig.set(map);
    this.persist(map);
  }

  /** Suggested pages for palette / sidebar — usage + light time-of-day boost. */
  readonly suggested = computed((): SearchIndexEntry[] => {
    const hour = new Date().getHours();
    const morning = hour >= 5 && hour < 12;
    const evening = hour >= 17 || hour < 5;
    const boostRoutes = morning
      ? ['/app/dashboard', '/app/audit/capa', '/app/tasks', '/app/me']
      : evening
        ? ['/app/reports/compliance', '/app/analytics', '/app/reports/custom']
        : [];

    const scored = Object.values(this.usageSig()).map((u) => {
      let score = u.count;
      if (u.route && boostRoutes.some((r) => u.route === r || u.route!.startsWith(r + '/'))) {
        score += 5;
      }
      return { u, score };
    });
    scored.sort((a, b) => b.score - a.score || b.u.lastAccess - a.u.lastAccess);

    const out: SearchIndexEntry[] = [];
    for (const { u } of scored.slice(0, 6)) {
      const item = u.id
        ? this.query.findItem(u.id)
        : u.route
          ? this.query.findByRoute(u.route)
          : null;
      if (!item?.route) continue;
      out.push({
        id: item.id,
        title: item.title,
        subtitle: 'Suggested',
        route: item.route,
        icon: sidebarIcon(item.icon),
        category: 'suggested',
        keywords: [item.title],
        moduleId: item.moduleId,
        badgeKey: item.badgeKey,
      });
    }
    return out;
  });

  /** Continue where you left off — most recent non-current route. */
  continueWhereLeftOff(currentUrl: string): { label: string; route: string } | null {
    const path = currentUrl.split(/[?#]/)[0].replace(/\/+$/, '') || '/';
    const hit = this.history.recent().find((r) => {
      const p = r.route.replace(/\/+$/, '') || '/';
      return p !== path;
    });
    return hit ? { label: hit.label, route: hit.route } : null;
  }

  private storageKey(): string {
    const uid = this.session.getUserId()?.trim();
    return uid ? `${USAGE_KEY}.${uid}` : USAGE_KEY;
  }

  private load(): Record<string, UsageStat> {
    if (!isPlatformBrowser(this.platformId)) return {};
    try {
      const raw = localStorage.getItem(this.storageKey()) ?? localStorage.getItem(USAGE_KEY);
      if (!raw) return {};
      const parsed = JSON.parse(raw) as Record<string, UsageStat>;
      return parsed && typeof parsed === 'object' ? parsed : {};
    } catch {
      return {};
    }
  }

  private persist(map: Record<string, UsageStat>): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(this.storageKey(), JSON.stringify(map));
    } catch {
      /* ignore */
    }
  }
}
