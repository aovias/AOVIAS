import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SessionService } from '../../auth/session.service';
import { FavoritesRecentService } from '../../services/favorites-recent.service';
import { NavigationQueryService } from '../engine/navigation-query.service';

export interface FavoriteEntry {
  readonly id?: string;
  readonly label: string;
  readonly route: string;
  readonly visitedAt: number;
}

const ORDER_KEY = 'praman.nav.favoritesOrder.v1';
const MAX_FAVORITES = 20;

/**
 * Ordered favorites on top of FavoritesRecentService storage.
 * Registry remains SSOT for labels via Query resolution.
 */
@Injectable({ providedIn: 'root' })
export class FavoritesService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly session = inject(SessionService);
  private readonly legacy = inject(FavoritesRecentService);
  private readonly query = inject(NavigationQueryService);

  private readonly orderSig = signal<string[]>(this.loadOrder());

  readonly favorites = computed((): FavoriteEntry[] => {
    const legacy = this.legacy.favorites();
    const byRoute = new Map(legacy.map((f) => [f.route, f]));
    const order = this.orderSig();
    const ordered: FavoriteEntry[] = [];
    for (const key of order) {
      const hit =
        byRoute.get(key) ?? legacy.find((f) => this.query.findItem(key)?.route === f.route);
      if (hit) {
        const item = this.query.findByRoute(hit.route);
        ordered.push({
          id: item?.id,
          label: item?.title ?? hit.label,
          route: hit.route,
          visitedAt: hit.visitedAt,
        });
        byRoute.delete(hit.route);
      }
    }
    for (const f of byRoute.values()) {
      const item = this.query.findByRoute(f.route);
      ordered.push({
        id: item?.id,
        label: item?.title ?? f.label,
        route: f.route,
        visitedAt: f.visitedAt,
      });
    }
    return ordered;
  });

  isFavorite(route: string): boolean {
    return this.legacy.isFavorite(route);
  }

  toggle(route: string, label: string, id?: string): void {
    const was = this.legacy.isFavorite(route);
    this.legacy.toggleFavorite({ route, label });
    const key = id ?? route;
    if (was) {
      this.orderSig.update((o) => o.filter((x) => x !== key && x !== route));
    } else {
      this.orderSig.update((o) =>
        [key, ...o.filter((x) => x !== key && x !== route)].slice(0, MAX_FAVORITES),
      );
    }
    this.persistOrder();
  }

  reorder(fromIndex: number, toIndex: number): void {
    const list = [...this.favorites()];
    if (fromIndex < 0 || toIndex < 0 || fromIndex >= list.length || toIndex >= list.length) return;
    const [moved] = list.splice(fromIndex, 1);
    list.splice(toIndex, 0, moved);
    this.orderSig.set(list.map((f) => f.id ?? f.route));
    this.persistOrder();
  }

  move(route: string, direction: -1 | 1): void {
    const list = this.favorites();
    const idx = list.findIndex((f) => f.route === route);
    if (idx < 0) return;
    this.reorder(idx, Math.max(0, Math.min(list.length - 1, idx + direction)));
  }

  private orderKey(): string {
    const uid = this.session.getUserId()?.trim();
    return uid ? `${ORDER_KEY}.${uid}` : ORDER_KEY;
  }

  private loadOrder(): string[] {
    if (!isPlatformBrowser(this.platformId)) return [];
    try {
      const raw = localStorage.getItem(this.orderKey()) ?? localStorage.getItem(ORDER_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as string[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private persistOrder(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(this.orderKey(), JSON.stringify(this.orderSig()));
    } catch {
      /* ignore */
    }
  }
}
