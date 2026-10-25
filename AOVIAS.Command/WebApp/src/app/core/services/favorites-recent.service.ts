import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationHistoryService } from '../navigation/engine/navigation-history.service';

export interface NavigationBookmark {
  label: string;
  route: string;
  visitedAt: number;
}

const FAVORITES_KEY = 'praman.nav.favorites.v1';
const MAX_FAVORITES = 20;

/**
 * Local favorites storage. Recent visits delegate to NavigationHistoryService (NAV-4 SSOT).
 */
@Injectable({ providedIn: 'root' })
export class FavoritesRecentService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly history = inject(NavigationHistoryService);

  readonly favorites = signal<NavigationBookmark[]>([]);

  /** Proxy recent from history SSOT. */
  readonly recent = this.history.recent;

  constructor() {
    this.favorites.set(this.readFavorites());
  }

  isFavorite(route: string): boolean {
    return this.favorites().some((f) => f.route === route);
  }

  toggleFavorite(item: Omit<NavigationBookmark, 'visitedAt'>): void {
    const current = [...this.favorites()];
    const idx = current.findIndex((f) => f.route === item.route);
    if (idx >= 0) {
      current.splice(idx, 1);
    } else {
      current.unshift({ ...item, visitedAt: Date.now() });
      if (current.length > MAX_FAVORITES) current.length = MAX_FAVORITES;
    }
    this.favorites.set(current);
    this.writeFavorites(current);
  }

  trackVisit(item: Omit<NavigationBookmark, 'visitedAt'>): void {
    this.history.trackVisit(item);
  }

  private readFavorites(): NavigationBookmark[] {
    if (!isPlatformBrowser(this.platformId)) return [];
    try {
      const raw = localStorage.getItem(FAVORITES_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as NavigationBookmark[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private writeFavorites(value: NavigationBookmark[]): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(FAVORITES_KEY, JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }
}
