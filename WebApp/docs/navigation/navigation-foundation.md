# AOVIAS Command — Navigation Foundation (NAV-1)

This document describes the enterprise navigation foundation introduced in Sprint NAV-1. It covers layout density, sidebar state, breakpoints, accessibility, and extension points for NAV-2 (Header & Module Navigation) and NAV-3 (Role-Based Dynamic Navigation).

**This sprint did not change:** information hierarchy, routes, business logic, permissions, navigation order, or page content.

---

## Package layout

```
WebApp/src/app/core/navigation/
  index.ts
  sidebar/
    sidebar.models.ts              # Config-driven menu registry + extension types
    sidebar.state.ts               # Storage keys + UI state helpers
    sidebar.service.ts             # Facade: roles → memoized menu, peek, recent, mode
    sidebar.component.ts           # Shell composition host
    sidebar-item.component.ts
    sidebar-group.component.ts
    favorites-section.component.ts
    recent-section.component.ts
    collapse-toggle.component.ts
```

Compatibility re-exports:

| Legacy path | Now |
|-------------|-----|
| `core/layout/sidebar-nav.model.ts` | Re-exports `navigation/sidebar/sidebar.models` |
| `core/layout/sidebar.component.ts` | Re-exports `navigation/sidebar/sidebar.component` |

Icons, badges, AI assistant, and compliance progress remain under `core/layout/` and are imported by the navigation package.

---

## Density tokens

Defined in `styles/tokens/foundation.tokens.css`:

| Token | Value |
|-------|------:|
| `--sidebar-width-expanded` | 240px |
| `--sidebar-width-collapsed` | 72px |
| `--sidebar-item-height` | 38px |
| `--sidebar-icon-size` / `--icon-size-nav` | 18px |
| `--sidebar-item-pad-x` | 12px |
| `--sidebar-section-gap` | 16px |
| `--sidebar-section-header-size` | 12px |
| `--sidebar-motion-duration` | 180ms |

Section headers: uppercase, letter-spacing, muted text token. Each section has title + hairline divider + consistent gap.

---

## Sidebar states

| State | Width | Labels | Persistence |
|-------|------:|--------|-------------|
| Expanded | 240px | Visible | User preference via `praman-sidebar-collapsed=0` |
| Collapsed | 72px | Hidden; `matTooltip` on items | `praman-sidebar-collapsed=1` |
| Peek | 240px temporary | Visible while pointer is over rail | **Not** persisted |

Peek is owned by `ShellSidebarService.setPeekExpanded()` and exposed through `SidebarService`. Intentional collapse still uses the footer rail toggle and the in-sidebar collapse control (`app-sidebar-collapse-toggle`).

Additional persisted UI:

| Key | Purpose |
|-----|---------|
| `praman-sidebar-mode` | `nav` \| `assistant` |
| `praman.nav.recentExpanded.v1` | Recent section open/closed |
| `praman.nav.favorites.v1` | Favorites (unchanged service) |
| `praman.nav.recent.v1` | Recent visits (unchanged service) |

---

## Favorites & Recent

Replaces the single “Favorites & recent” block:

1. **★ Favorites** — always expanded; unpin control retained.
2. **Recent (N)** — collapsible, top **5** items, chevron animation, expand state persisted.

Data APIs remain in `FavoritesRecentService` (no visit-tracking changes).

---

## Breakpoints (NAV-1)

| Viewport | Behavior |
|----------|----------|
| >1600 | In-flow expanded rail (240) |
| 1400–1600 | Expanded (240); fixed rail until 1600 |
| 1024–1399 | Laptop: **collapsed by default** on first visit if no storage preference |
| 768–1023 | Overlay drawer |
| <768 | Mobile drawer + bottom nav |

Constants live in `shell-sidebar.service.ts` (`SHELL_SIDEBAR_*`). Content padding uses the fixed-rail band (1024–1599) so the page does not horizontal-scroll.

**Previous map (retired):** overlay ≤767; narrow 260px at 1024–1280; tablet inline through 1919; expanded 280px.

---

## Menu configuration

Source of truth (NAV-3): `MENU_REGISTRY` in `registry/menu.registry.ts`.  
Legacy shim: `getSidebarMenuSections()` in `sidebar.models.ts` (deprecated).

Current sidebar sections (order unchanged):

- `dashboard` — Dashboard
- `compliance-execution` — Compliance Execution
- `action-center` — Action Center

Header mega-menu groups remain in `getHeaderNavGroups()` (Reports, Audit, Knowledge, etc.). Administration / System sidebar groups are **not invented** until registry items exist.

Filtering today: `filterSidebarMenuSections()` + optional `requiredRoles` on items. Stable `item.id` values are assigned via `withNavItemIds()` / `navItemIdFromRoute()`.

---

## Extension points (for NAV-2 / NAV-3)

Typed but unused in NAV-1:

```ts
interface NavResolveContext {
  roles: readonly string[];
  permissions?: readonly string[];
  workspaceId?: string | null;
}

type NavItemResolver = (item: SidebarMenuItem, ctx: NavResolveContext) => boolean;

// On SidebarMenuItem:
filter?: NavItemResolver;  // future dynamic/permission predicate
id?: string;               // analytics, pinning, dynamic menus
```

Planned consumers (not implemented here):

- Role-based / permission-based navigation
- Dynamic menus & favorite pinning UX
- Global search entry / workspace switching
- Module navigation (NAV-2 header)

Avoid hardcoding parallel menu lists in components — extend the registry and `SidebarService.resolveMenuSections()`.

---

## Accessibility

- Section `aria-labelledby`
- Recent toggle: `aria-expanded` / `aria-controls`
- Collapse toggle: `aria-pressed` + clear labels
- Overlay: `cdkTrapFocus`, Escape closes drawer
- `:focus-visible` rings on menu items, toggles, bookmarks
- Tooltips enabled only when labels are hidden (`labelsHidden`)
- `aria-label` on icon-only controls

Motion: CSS 150–200ms; disabled under `prefers-reduced-motion: reduce`.

---

## Performance

- `ChangeDetectionStrategy.OnPush` on all nav components
- Signals / `computed` for rail width, labels visibility, memoized menu tree
- `@for` track by `item.id ?? item.route`
- Presentational children receive inputs; shell owns hospital/context side effects

---

## Header (NAV-1 polish)

Top mega-nav keeps the existing **More** overflow pattern (`ResizeObserver` in `header-top-nav.component.ts`). No IA redesign. Header rail CSS defaults align to **240px** (`--header-rail-px`) so brand column tracks the denser sidebar.

---

## Migration notes

### UI-only changes

- Sidebar width 280/260 → **240**; collapsed remains **72**
- Item height / padding / icon size densified (~30% less vertical chrome)
- Favorites and Recent split into two sections; Recent collapsible
- Collapse control also in sidebar header (footer toggle still works)
- Peek-on-hover when collapsed
- Breakpoint map updated (see above)
- Components extracted under `core/navigation/sidebar/`

### What did not change

- Routes, labels order, `requiredRoles` semantics
- Page components and business logic
- Theme colors / dark mode tokens (no new brand colors)

### Import guidance

Prefer:

```ts
import { SidebarComponent, SidebarService, NavigationQueryService } from '@app/core/navigation';
// or relative: '../../navigation'
```

Legacy `core/layout/sidebar-nav.model` and `sidebar.component` re-exports remain for the transition.

### Follow-on sprints

- **NAV-2** — Header & module navigation — see [header-navigation.md](./header-navigation.md)
- **NAV-3** — Data-driven framework — see [navigation-framework.md](./navigation-framework.md)

---

## Related docs

- [Navigation Framework (NAV-3)](./navigation-framework.md)
- [Menu Registry](./menu-registry.md)
- [Navigation Engine](./navigation-engine.md)
- [Shell sidebar IA (legacy notes)](../architecture/sidebar-navigation.md) — section order and badge keys; see also this foundation doc for runtime architecture.
