import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SessionService } from '../../auth/session.service';
import { NavigationQueryService } from '../engine/navigation-query.service';
import { NavigationStateService } from '../engine/navigation-state.service';

export interface PinnedEntry {
  readonly id: string;
  readonly label: string;
  readonly route: string;
}

const PINNED_KEY = 'praman.nav.pinned.v1';

/**
 * Pinned items — "I want visible" (distinct from favorites "I use often").
 * Always shown at top of sidebar when non-empty.
 */
@Injectable({ providedIn: 'root' })
export class PinnedService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly session = inject(SessionService);
  private readonly query = inject(NavigationQueryService);
  private readonly navState = inject(NavigationStateService);

  private readonly idsSig = signal<string[]>(this.load());

  constructor() {
    queueMicrotask(() => this.syncState());
  }

  readonly pinnedIds = this.idsSig.asReadonly();

  readonly pinned = computed((): PinnedEntry[] =>
    this.idsSig()
      .map((id) => {
        const item = this.query.findItem(id) ?? this.query.findByRoute(id);
        if (!item?.route) return null;
        return { id: item.id, label: item.title, route: item.route };
      })
      .filter((x): x is PinnedEntry => x != null),
  );

  isPinned(idOrRoute: string): boolean {
    if (this.idsSig().includes(idOrRoute)) return true;
    const item = this.query.findByRoute(idOrRoute) ?? this.query.findItem(idOrRoute);
    return !!item && this.idsSig().includes(item.id);
  }

  toggle(idOrRoute: string): void {
    const item = this.query.findItem(idOrRoute) ?? this.query.findByRoute(idOrRoute);
    const id = item?.id ?? idOrRoute;
    if (this.idsSig().includes(id)) {
      this.idsSig.update((ids) => ids.filter((x) => x !== id));
    } else {
      this.idsSig.update((ids) => [id, ...ids.filter((x) => x !== id)].slice(0, 12));
    }
    this.persist();
    this.syncState();
  }

  private syncState(): void {
    this.navState.setPinnedItemIds(this.idsSig());
  }

  private storageKey(): string {
    const uid = this.session.getUserId()?.trim();
    return uid ? `${PINNED_KEY}.${uid}` : PINNED_KEY;
  }

  private load(): string[] {
    if (!isPlatformBrowser(this.platformId)) return [];
    try {
      const raw = localStorage.getItem(this.storageKey()) ?? localStorage.getItem(PINNED_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as string[];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  private persist(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(this.storageKey(), JSON.stringify(this.idsSig()));
    } catch {
      /* ignore */
    }
  }
}
