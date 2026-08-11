# Menu Registry

How to add modules, categories, and navigation items under NAV-3.

Related: [Navigation Framework](./navigation-framework.md) · [Navigation Engine](./navigation-engine.md)

---

## Files

| File | Contents |
|------|----------|
| `registry/menu.registry.ts` | Flat `NavigationItem[]` (`MENU_REGISTRY`) |
| `registry/module.registry.ts` | `NavigationModule[]` + Compliance Graph roles |
| `registry/category.registry.ts` | Sidebar/header/bottom category titles + order |
| `registry/feature.registry.ts` | Feature keys + default enabled map |
| `registry/permission.registry.ts` | Permission key constants (metadata only) |
| `registry/route-match.util.ts` | Path normalize / shell path helpers / `matchRoute` |

Icons stay in `core/layout/sidebar-icons.registry.ts` (semantic id → SVG). Registry stores semantic icon ids only.

---

## IDs

Use dotted constants, stable across releases:

- Items: `workspace.dashboard.overview`, `compliance-graph.workspace`, `reports.compliance`
- Modules: `module.patient-safety`, `module.audit`
- Categories: `dashboard`, `compliance-execution`, `action-center`

Never derive identity from array index. Sort with explicit `order` (10, 20, 30…).

---

## Adding a sidebar item

1. Append a frozen item in `MENU_REGISTRY` via the local `navItem()` helper.
2. Set `surfaces: ['sidebar', 'search', 'breadcrumb']` (or include `'bottom'` only for mobile shortcuts).
3. Set `categoryId` to an existing sidebar category.
4. Set `moduleId: NavigationModuleIds.Workspace` for operational rail items.
5. Optional: `roleKeys`, `badgeKey`, `featureKey`, `subsetActive`, `tooltip`.

Example shape:

```ts
navItem({
  id: 'workspace.dashboard.example',
  title: 'Example',
  icon: 'overview',
  moduleId: NavigationModuleIds.Workspace,
  route: '/app/example',
  order: 100,
  categoryId: NavigationCategoryIds.Dashboard,
  surfaces: ['sidebar', 'search', 'breadcrumb'],
});
```

---

## Adding a header module

1. Add `NavigationModule` to `MODULE_REGISTRY` (`order`, `defaultRoute`, optional `roleKeys` / `featureKey`).
2. Add child items in `MENU_REGISTRY` with `surfaces` including `'header'` and matching `moduleId`.
3. Module Switcher picks them up via `HeaderProvider` → `NavigationQueryService.headerModules()`.

---

## Flags on `NavigationItem`

| Field | Meaning |
|-------|---------|
| `visible` / `enabled` | Base visibility (Engine also applies feature flags) |
| `searchable` | Included in `SearchProvider` |
| `favoriteAllowed` / `recentTracking` | Favorites / Recent eligibility |
| `breadcrumb` | Included in breadcrumb resolution |
| `roleKeys` | At least one role required (same semantics as former `requiredRoles`) |
| `badgeKey` | Resolved by `NAV_BADGE_PROVIDERS` |
| `featureKey` | Gated by feature-flag provider |

---

## Compatibility

Deprecated builders in `sidebar.models.ts` map registry → old `SidebarMenuSection` / `HeaderNavGroup` shapes. Prefer Query/Providers for all new work.
