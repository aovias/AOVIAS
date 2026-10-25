import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {
  readRecentExpanded,
  readSidebarViewMode,
  writeRecentExpanded,
  writeSidebarViewMode,
  type SidebarViewMode,
} from '../sidebar/sidebar.state';

/**
 * UI chrome state for navigation (expanded/collapsed/selected/pinned).
 * Storage keys match NAV-1 sidebar state for continuity.
 */
@Injectable({ providedIn: 'root' })
export class NavigationStateService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly storage = isPlatformBrowser(this.platformId) ? localStorage : null;

  readonly recentExpanded = signal(readRecentExpanded(this.storage, true));
  readonly viewMode = signal<SidebarViewMode>(readSidebarViewMode(this.storage, 'nav'));
  readonly selectedItemId = signal<string | null>(null);
  readonly pinnedItemIds = signal<readonly string[]>([]);
  readonly hiddenItemIds = signal<readonly string[]>([]);

  setViewMode(mode: SidebarViewMode): void {
    this.viewMode.set(mode);
    writeSidebarViewMode(this.storage, mode);
  }

  toggleRecentExpanded(): void {
    this.recentExpanded.update((v) => {
      const next = !v;
      writeRecentExpanded(this.storage, next);
      return next;
    });
  }

  setRecentExpanded(expanded: boolean): void {
    this.recentExpanded.set(expanded);
    writeRecentExpanded(this.storage, expanded);
  }

  selectItem(id: string | null): void {
    this.selectedItemId.set(id);
  }

  setPinnedItemIds(ids: readonly string[]): void {
    this.pinnedItemIds.set(ids);
  }
}
