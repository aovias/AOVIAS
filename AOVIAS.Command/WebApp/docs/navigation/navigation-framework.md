# AOVIAS Command — Navigation Framework (NAV-3)

Data-driven navigation: immutable registries as the single source of truth, an Engine that builds surface trees, and Query/Provider APIs consumed by shell UI.

**Does not change:** routes, IA order, or current role visibility behavior. Permission evaluation and real feature-flag backends remain extension points (stubs today).

Related:

- [Navigation Foundation (NAV-1)](./navigation-foundation.md)
- [Header Navigation (NAV-2)](./header-navigation.md)
- [Menu Registry](./menu-registry.md)
- [Navigation Engine](./navigation-engine.md)
- [Intelligent Navigation (NAV-4)](./intelligent-navigation.md)
- [Global Search](./global-search.md)
- [Personalization](./personalization.md)
- [Keyboard Shortcuts](./keyboard-shortcuts.md)

---

## Principles

1. **Registry SSOT** — Menus live in frozen registries under `core/navigation/registry/`, not in components.
2. **UI does not filter** — Surfaces render what Query returns; roles/features/permissions run in the Engine.
3. **Signals + cache** — Trees recompute when `NavigationContext` changes; not on every `NavigationEnd`.
4. **Plugin-ready** — `NavigationEngineService.registerNavigationExtension()` merges in-memory modules/items for future marketplace modules.

---

## Architecture

```
Immutable Registries → NavigationEngine → NavigationCache
                              ↓
                     NavigationQueryService
                              ↓
        Sidebar / Header / Breadcrumb / Search / Favorites providers
                              ↓
                     Shell UI (sidebar, module switcher, bottom nav, …)
```

Bootstrap: `provideNavigationFramework()` in `app.config.ts` wires visibility and badge tokens.

---

## Package layout

```
WebApp/src/app/core/navigation/
  registry/          # MENU / MODULE / CATEGORY / FEATURE / PERMISSION
  models/            # NavigationItem, Module, Badge, Feature, Permission
  engine/            # Engine, Query, State, Cache, History + providers/
  surfaces/          # Sidebar, Header, Breadcrumb, Search, Favorites providers
  sidebar/           # Thin UI consumers (NAV-1)
  header/            # Thin UI consumers (NAV-2)
```

---

## Public API (from `core/navigation`)

| Export | Role |
|--------|------|
| `NavigationQueryService` | `sidebarSections()`, `headerModules()`, `bottomItems()`, `breadcrumbForUrl`, `searchItems`, `findByRoute`, `titleForUrl`, … |
| `NavigationEngineService` | Pipeline + `registerNavigationExtension` |
| `SidebarProvider` / `HeaderProvider` / … | Surface adapters |
| `MENU_REGISTRY` / `MODULE_REGISTRY` / … | Frozen SSOT |
| `NAV_*` tokens | Role / permission / feature / badge injection points |

Legacy `getSidebarMenuSections()` / `getHeaderNavGroups()` remain as **deprecated shims** over the registry for any leftover non-UI imports. Shell UI must not call them.
