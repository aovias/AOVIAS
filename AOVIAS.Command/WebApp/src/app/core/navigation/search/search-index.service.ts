import { Injectable, computed, inject } from '@angular/core';
import { sidebarIcon } from '../../layout/sidebar-icons.registry';
import { NavigationQueryService } from '../engine/navigation-query.service';
import {
  COMMAND_ACTIONS,
  type SearchIndexEntry,
  type SearchResultCategory,
} from './search-sources';

export interface SearchGroupedResults {
  readonly category: SearchResultCategory;
  readonly label: string;
  readonly items: readonly SearchIndexEntry[];
}

const CATEGORY_LABELS: Record<SearchResultCategory, string> = {
  pages: 'Pages',
  modules: 'Modules',
  actions: 'Actions',
  settings: 'Settings',
  favorites: 'Favorites',
  suggested: 'Suggested',
  ops: 'Ops / Entities',
  departments: 'Departments',
  audits: 'Audits',
  reports: 'Reports',
  documents: 'Documents',
  users: 'Users',
};

const DISPLAY_ORDER: SearchResultCategory[] = [
  'favorites',
  'suggested',
  'pages',
  'modules',
  'actions',
  'settings',
  'ops',
];

@Injectable({ providedIn: 'root' })
export class SearchIndexService {
  private readonly query = inject(NavigationQueryService);

  /** Full index — recomputes when query surface trees change (roles/features). */
  readonly index = computed((): readonly SearchIndexEntry[] => {
    const pages: SearchIndexEntry[] = this.query
      .searchItems('')
      .filter((i) => i.route)
      .map((i) => ({
        id: i.id,
        title: i.title,
        subtitle: i.route,
        route: i.route,
        icon: sidebarIcon(i.icon),
        category:
          i.moduleId === 'module.settings' || i.route?.startsWith('/app/settings')
            ? ('settings' as const)
            : ('pages' as const),
        keywords: [i.title, i.route ?? '', i.description ?? '', i.moduleId],
        moduleId: i.moduleId,
        badgeKey: i.badgeKey,
      }));

    const modules: SearchIndexEntry[] = this.query.headerModules().map((m) => ({
      id: `mod-${m.id}`,
      title: m.title,
      subtitle: 'Module',
      route: m.route,
      icon: sidebarIcon(m.icon),
      category: 'modules' as const,
      keywords: [m.title, m.route, m.id],
      moduleId: m.id,
    }));

    const actions: SearchIndexEntry[] = COMMAND_ACTIONS.map((a) => ({
      id: a.id,
      title: a.title,
      subtitle: a.subtitle,
      route: a.route,
      category: a.category,
      keywords: [...a.keywords, a.title],
      actionId: a.id,
    }));

    return Object.freeze([...pages, ...modules, ...actions]);
  });

  search(query: string, extras: readonly SearchIndexEntry[] = []): SearchGroupedResults[] {
    const q = query.trim().toLowerCase();
    const pool = [...this.index(), ...extras];
    const matched = q
      ? pool.filter((e) => this.matches(e, q))
      : pool.filter((e) => e.category === 'pages' || e.category === 'modules').slice(0, 12);

    const byCat = new Map<SearchResultCategory, SearchIndexEntry[]>();
    for (const item of matched) {
      const list = byCat.get(item.category) ?? [];
      list.push(item);
      byCat.set(item.category, list);
    }

    return DISPLAY_ORDER.filter((c) => (byCat.get(c)?.length ?? 0) > 0).map((category) => ({
      category,
      label: CATEGORY_LABELS[category],
      items: Object.freeze((byCat.get(category) ?? []).slice(0, 8)),
    }));
  }

  categoryLabel(category: SearchResultCategory): string {
    return CATEGORY_LABELS[category];
  }

  private matches(entry: SearchIndexEntry, q: string): boolean {
    if (entry.title.toLowerCase().includes(q)) return true;
    if (entry.subtitle?.toLowerCase().includes(q)) return true;
    if (entry.route?.toLowerCase().includes(q)) return true;
    return entry.keywords.some((k) => k.toLowerCase().includes(q));
  }
}
