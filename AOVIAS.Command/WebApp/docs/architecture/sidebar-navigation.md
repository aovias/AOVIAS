# Shell sidebar navigation (IA)

> **NAV-1 foundation:** Runtime architecture, density tokens, breakpoints, Favorites/Recent, peek/collapse state, and extension points are documented in [`docs/navigation/navigation-foundation.md`](../navigation/navigation-foundation.md). Prefer that doc for shell implementation details.

**Code:** `src/app/core/navigation/sidebar/` (models, service, components). Legacy re-exports: `core/layout/sidebar-nav.model.ts`, `core/layout/sidebar.component.ts`. Icons/badges: `sidebar-icons.registry.ts`, `sidebar-badges.map.ts`.

## Section order (unchanged)

Sidebar registry sections currently rendered:

| Section `id`             | Title                        |
|--------------------------|------------------------------|
| `dashboard`              | Dashboard                    |
| `compliance-execution`   | Compliance Execution         |
| `action-center`          | Action Center                |

Reports, Audit, Knowledge, and related modules live in the **header mega-menu** (`getHeaderNavGroups()`), not the left rail. Do not invent Administration / System sidebar groups until items exist in the registry.

Routes are the single source of truth in `getSidebarMenuSections()` — **do not** hardcode parallel lists in UI components. **Mobile bottom nav** uses `getBottomNavItems()` derived from the same registry (subset, max five entries).

## Icon registry

Semantic IDs live in `sidebar-icons.registry.ts` (`SidebarSemanticIconId`). **Compliance Execution** uses `journey`, `standardsChapters`, `evidence`, `tasks`. **Action Center** uses `findings`, `issuesRisks` (shield + alert stroke icon), `alertsNotifications`.

Rendered size is controlled by **`--icon-size-nav`** on `.menu-icon` / bottom nav — not inline SVG pixel sizes.

## Action Center badges

| `navBadgeKey` (on item) | Variant (token)   | Route (informational)        |
|-------------------------|-------------------|------------------------------|
| `findings`              | `critical`        | `/app/audit/findings`        |
| `issues`                | `warning`         | `/app/audit/corrective-actions` |
| `alerts`                | `neutral`         | `/app/control-center/tasks`  |

Counts are supplied by `SidebarComponent` (mock signals today; replace with API when available). Resolution is centralized in `resolveNavBadge()`.

## Quick actions strip

**Task**, **Evidence**, and **Finding** remain as **compact icon tiles** above the main nav (`quick-actions` in `sidebar.component.ts`). They use the same routes and **registry SVGs** as the corresponding rows in **Compliance Execution** / **Action Center** (`sidebarIcon('tasks' | 'evidence' | 'findings')`). They are **shortcuts**, not a second source of truth — routes stay defined only in `getSidebarMenuSections()`.

## Sidebar menu visibility

The main sidebar application navigation (`<nav class="sidebar-nav">`) is rendered by `SidebarComponent` and is gated by the `showSidebarApplicationNav` flag. In production this flag MUST remain `true` so that the menu list stays aligned with the information architecture and shared route registry defined in `getSidebarMenuSections()`. If temporary experiments require hiding the sidebar menu, prefer a dedicated feature flag or environment configuration over hard-coding this value.
