# Navigation Engine

Pipeline, providers, cache, and Query API for NAV-3.

Related: [Navigation Framework](./navigation-framework.md) · [Menu Registry](./menu-registry.md)

---

## Pipeline (fixed order)

For each resolve with a `NavigationContext`:

1. **Base visibility** — `item.visible`
2. **Feature flags** — `NAV_FEATURE_FLAGS` (defaults from `FEATURE_REGISTRY`; context overrides)
3. **Roles** — `NAV_ROLE_VISIBILITY` (item `roleKeys` + module gates such as Compliance Graph)
4. **Permissions** — `NAV_PERMISSION_VISIBILITY` (no-op stub today)
5. **Surface filter** — `sidebar` | `header` | `bottom` | …

Parent/child trees use `parentId` + `order` (never array position).

---

## Services

| Service | Responsibility |
|---------|----------------|
| `NavigationEngineService` | Load registries, apply pipeline, build surface snapshots, `registerNavigationExtension()` |
| `NavigationCacheService` | Cache trees keyed by context (roles / permissions / feature overrides) |
| `NavigationQueryService` | Signal-backed public API; syncs roles from `UserContextService` |
| `NavigationStateService` | Expanded / view-mode chrome (NAV-1 storage keys preserved) |
| `NavigationHistoryService` | Recent visits (`praman.nav.recent.v1`) |

Invalidate cache when context changes — not on every route navigation. Active title / active module still recompute from URL.

---

## Provider tokens

Registered by `provideNavigationFramework()`:

| Token | Default | Behavior |
|-------|---------|----------|
| `NAV_ROLE_VISIBILITY` | `DefaultRoleVisibilityProvider` | Mirrors former `requiredRoles` + Compliance Graph gate |
| `NAV_PERMISSION_VISIBILITY` | `NoOpPermissionVisibilityProvider` | Always allow |
| `NAV_FEATURE_FLAGS` | `DefaultFeatureFlagProvider` | All registered features enabled unless overridden / `enabled: false` |
| `NAV_BADGE_PROVIDERS` / `NAV_BADGE_RESOLVER` | Findings / Issues / Alerts adapters | Map `badgeKey` → `{ count, variant }` via `NavigationBadgeStore` |

Replace any provider in tests or future sprints without touching UI.

### Feature-flag test hook

```ts
inject(NavigationEngineService).withFeatureOverride('AIComplianceCopilot', false);
// or NavigationQueryService.setContext({ roles, featureOverrides: { … } })
```

---

## Query API

```ts
query.sidebarSections()      // NavigationSidebarSection[]
query.sidebarViewSections()  // SVG icon view model for SidebarService
query.headerModules()
query.bottomItems()
query.breadcrumbForUrl(url)
query.searchItems(q)
query.favoritesCandidates()
query.childrenOf(id) / parentOf(id)
query.findByRoute(url) / findItem(id) / findModule(id)
query.titleForUrl(url)
```

---

## Surface providers

| Provider | Consumer |
|----------|----------|
| `SidebarProvider` | `SidebarService.menuSections` |
| `HeaderProvider` | `ModuleSwitcherService` |
| `BreadcrumbProvider` | My Work, Reports landing (others optional) |
| `SearchProvider` | Future Global Ops Search |
| `FavoritesProvider` | Sidebar Favorites / Recent labels |

---

## Badges

Mock counts seed via `NavigationBadgeStore.seedMocks()` (same Findings=3 critical, CAPA=2 warning as before). UI calls `resolveNavBadge` against the store; production can swap badge providers without sidebar changes.
