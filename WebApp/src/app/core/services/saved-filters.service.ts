import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface SavedFilterBookmark {
  id: string;
  label: string;
  module: 'tasks' | 'evidence' | 'capa' | 'audits' | 'reports' | 'other';
  href: string;
  query?: Record<string, string>;
  savedAt: number;
}

const STORAGE_KEY = 'praman.productivity.savedFilters.v1';
const MAX_ITEMS = 24;

/**
 * Bookmarks / saved filters for personal productivity (Phase 3 Sprint 6).
 */
@Injectable({ providedIn: 'root' })
export class SavedFiltersService {
  private readonly platformId = inject(PLATFORM_ID);
  readonly items = signal<SavedFilterBookmark[]>([]);

  constructor() {
    this.items.set(this.read());
  }

  add(input: Omit<SavedFilterBookmark, 'id' | 'savedAt'>): void {
    const next: SavedFilterBookmark = {
      ...input,
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      savedAt: Date.now(),
    };
    const list = [next, ...this.items().filter((x) => x.href !== next.href)].slice(0, MAX_ITEMS);
    this.items.set(list);
    this.write(list);
  }

  remove(id: string): void {
    const list = this.items().filter((x) => x.id !== id);
    this.items.set(list);
    this.write(list);
  }

  clear(): void {
    this.items.set([]);
    this.write([]);
  }

  private read(): SavedFilterBookmark[] {
    if (!isPlatformBrowser(this.platformId)) return [];
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as SavedFilterBookmark[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private write(value: SavedFilterBookmark[]): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    } catch {
      /* ignore */
    }
  }
}
