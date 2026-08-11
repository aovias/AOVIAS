# Phase 3 — Navigation Experience (Sprint 6)

**Sprint:** 6  
**Status:** Delivered  
**Scope:** Breadcrumbs, favorites/recent UI, global ops search shortcut, sidebar surfacing.

## Breadcrumbs

- Shared `app-page-breadcrumb` (`WebApp/src/app/shared/ui/page-breadcrumb.component.ts`)  
- Used on Reports landing and My work (`/app/me`)  
- Pattern: Home → current page; `aria-current="page"` on the leaf

Admin module breadcrumbs (settings / workforce-admin / ai-identity) remain feature-local; operational modules should prefer `app-page-breadcrumb`.

## Favorites + recent

| Surface | Behavior |
|---------|----------|
| `FavoritesRecentService` | localStorage `praman.nav.favorites.v1` / `praman.nav.recent.v1` (Sprint 1) |
| Sidebar | **Favorites & recent** section above Application nav; star/unstar from recent |
| Header | Star button → panel with favorites, recent, link to **My work** |
| Tracking | Sidebar `NavigationEnd` records humanized labels |

## Global ops search (Ctrl/Cmd+K)

- Component: `global-ops-search.component.ts`  
- **Ctrl+K** / **⌘K** focuses the header search input and opens the panel  
- Placeholder documents the shortcut; `aria-keyshortcuts="Control+K Meta+K"`  
- Enter while focused saves the current query (existing Sprint 1 behavior)

## Related routes

| Route | Purpose |
|-------|---------|
| `/app/dashboard` | Role-aware home (NBA strip) |
| `/app/me` / `/app/productivity` | Personal productivity facade |
| `/app/reports` | Role-filtered report catalog |

## Keyboard shortcuts (documented)

| Shortcut | Action |
|----------|--------|
| Ctrl/⌘ + K | Focus global operational search |
| Esc | Close overlay sidebar (existing) |
| Enter (in ops search) | Save current search query |
