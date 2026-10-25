import { Injectable, computed, inject } from '@angular/core';
import { FavoritesService } from '../favorites/favorites.service';
import { NavigationHistoryService } from '../engine/navigation-history.service';
import { NavigationQueryService } from '../engine/navigation-query.service';
import { WorkspacePreferencesService } from '../personalization/workspace-preferences.service';

export interface ResolvedNavBookmark {
  label: string;
  route: string;
  visitedAt: number;
  id?: string;
}

@Injectable({ providedIn: 'root' })
export class FavoritesProvider {
  private readonly favoritesSvc = inject(FavoritesService);
  private readonly history = inject(NavigationHistoryService);
  private readonly query = inject(NavigationQueryService);
  private readonly prefs = inject(WorkspacePreferencesService);

  readonly favorites = computed((): ResolvedNavBookmark[] =>
    this.favoritesSvc.favorites().map((f) => ({
      id: f.id,
      label: f.label,
      route: f.route,
      visitedAt: f.visitedAt,
    })),
  );

  readonly recent = computed((): ResolvedNavBookmark[] => {
    const max = this.prefs.maxRecentItems();
    return this.history
      .recent()
      .filter((r) => {
        const item = this.query.findByRoute(r.route);
        return !item || item.recentTracking;
      })
      .slice(0, max)
      .map((r) => {
        const item = this.query.findByRoute(r.route);
        return {
          id: item?.id ?? r.id,
          label: item?.title ?? r.label,
          route: r.route,
          visitedAt: r.visitedAt,
        };
      });
  });

  candidates() {
    return this.query.favoritesCandidates();
  }
}
