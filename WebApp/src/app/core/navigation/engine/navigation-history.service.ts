import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SessionService } from '../../auth/session.service';
import { WorkspacePreferencesService } from '../personalization/workspace-preferences.service';

export interface NavigationHistoryEntry {
  readonly id?: string;
  readonly label: string;
  readonly route: string;
  readonly moduleId?: string;
  readonly visitedAt: number;
}

const RECENT_KEY = 'praman.nav.recent.v1';
const SEARCH_KEY = 'praman.nav.recentSearches.v1';
const DEFAULT_MAX = 10;

/**
 * Recent route/module visits + recent searches (NAV-4 SSOT).
 * Shares `praman.nav.recent.v1` with FavoritesRecentService for continuity.
 */
@Injectable({ providedIn: 'root' })
export class NavigationHistoryService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly session = inject(SessionService);
  private readonly prefs = inject(WorkspacePreferencesService);

  readonly recent = signal<NavigationHistoryEntry[]>([]);
  readonly recentSearches = signal<string[]>([]);

  constructor() {
    this.recent.set(this.readRecent());
    this.recentSearches.set(this.readSearches());
  }

  trackVisit(entry: Omit<NavigationHistoryEntry, 'visitedAt'>): void {
    const max = this.prefs.maxRecentItems() || DEFAULT_MAX;
    const next: NavigationHistoryEntry[] = [
      { ...entry, visitedAt: Date.now() },
      ...this.recent().filter((r) => r.route !== entry.route),
    ].slice(0, max);
    this.recent.set(next);
    this.writeRecent(next);
  }

  trackModuleVisit(moduleId: string, label: string, route: string): void {
    this.trackVisit({ id: moduleId, label, route, moduleId });
  }

  trackSearch(query: string): void {
    const q = query.trim();
    if (q.length < 2) return;
    const max = this.prefs.prefs().searchHistoryRetention || 12;
    const next = [q, ...this.recentSearches().filter((s) => s !== q)].slice(0, max);
    this.recentSearches.set(next);
    this.writeSearches(next);
  }

  private recentKey(): string {
    const uid = this.session.getUserId()?.trim();
    return uid ? `${RECENT_KEY}.${uid}` : RECENT_KEY;
  }

  private searchKey(): string {
    const uid = this.session.getUserId()?.trim();
    return uid ? `${SEARCH_KEY}.${uid}` : SEARCH_KEY;
  }

  private readRecent(): NavigationHistoryEntry[] {
    if (!isPlatformBrowser(this.platformId)) return [];
    try {
      const raw = localStorage.getItem(this.recentKey()) ?? localStorage.getItem(RECENT_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as NavigationHistoryEntry[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private writeRecent(value: NavigationHistoryEntry[]): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(this.recentKey(), JSON.stringify(value));
      // Keep legacy key in sync for FavoritesRecentService readers
      localStorage.setItem(RECENT_KEY, JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }

  private readSearches(): string[] {
    if (!isPlatformBrowser(this.platformId)) return [];
    try {
      const raw = localStorage.getItem(this.searchKey()) ?? localStorage.getItem(SEARCH_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as string[];
      return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string') : [];
    } catch {
      return [];
    }
  }

  private writeSearches(value: string[]): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(this.searchKey(), JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }
}
