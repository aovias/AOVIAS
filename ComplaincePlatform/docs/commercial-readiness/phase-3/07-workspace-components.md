# Phase 3 — Workspace Components (Sprint 1)

**Sprint:** 1  
**Status:** Delivered  
**Scope:** Shared analytics UI primitives + role-aware shell navigation for the Role Experience Framework.

## Shared UI (`WebApp/src/app/shared/ui/analytics/`)

| Component | Selector | Purpose |
|-----------|----------|---------|
| KPI card | `app-kpi-card` | Compact metric tile (label / value / optional tone) |
| Priority NBA strip | `app-priority-nba-strip` | Next-best-action list from Role Experience home priorities |
| Risk list | `app-risk-list` | Thin attention / risk list |

Barrel export: `shared/ui/analytics/index.ts`.

## Role-aware sidebar

- `SidebarMenuItem.requiredRoles?: string[]` in `sidebar-nav.model.ts`
- `filterSidebarMenuSections(sections, userRoles)` hides items without a matching role
- `sidebar.component` filters via `UserContextService.roles$` (canonical + normalized role ids)

Examples: Framework catalog (admin/QM), Auditor home (EXTERNAL_AUDITOR / REGULATOR), OE ownership (admin/QM).

## Favorites + recent

`FavoritesRecentService` (`core/services/favorites-recent.service.ts`) persists:

- Favorites — `localStorage` key `praman.nav.favorites.v1`
- Recent — `praman.nav.recent.v1` (max 12; updated on `NavigationEnd` from the sidebar)

## Default landing

After login / MFA success:

1. Call `RoleExperienceService.resolveDefaultHomeRoute()` → GET `/role-experience/home`
2. On failure, use local pack map (`resolveLocalPack`)
3. Navigate to pack `defaultHomeRoute` (supports `?persona=` query)

## Dashboard integration

`dashboard.component`:

- Loads GET `/role-experience/home` into NBA strip at top
- `showWidget()` prefers pack `widgetAllowList`, else merges experience `role-pack:*` permissions with persona widget map
- Default persona: Department Head aliases win over MANAGER → Quality Manager; query `?persona=` overrides
- Pack id from home API syncs persona tabs when query is absent

## Gateway client path

`RoleExperienceService` extends `BaseApiService` and calls:

```text
GET {apiBaseUrl}/role-experience/home
```

(`apiBaseUrl` is `/api/v1` via environment / proxy).
