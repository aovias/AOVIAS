import { Injectable, inject } from '@angular/core';
import { SearchIndexService } from '../search/search-index.service';

export interface NavigationSearchHit {
  id: string;
  title: string;
  route: string;
  icon: string;
  moduleId: string;
}

/** Thin adapter over SearchIndexService for NAV-3 public API. */
@Injectable({ providedIn: 'root' })
export class SearchProvider {
  private readonly index = inject(SearchIndexService);

  search(query: string): NavigationSearchHit[] {
    return this.index
      .search(query)
      .flatMap((g) => g.items)
      .filter((i) => !!i.route)
      .map((i) => ({
        id: i.id,
        title: i.title,
        route: i.route ?? '/',
        icon: i.icon ?? '',
        moduleId: i.moduleId ?? '',
      }));
  }
}
