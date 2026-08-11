# Phase 57 — Research: Icon-first shell sidebar navigation

## Current implementation (baseline)

- **Registry:** `src/app/core/layout/sidebar-nav.model.ts` exports `SidebarMenuSection`, `SidebarMenuItem`, and `getSidebarMenuSections()`. Items use **inline SVG strings** in the `icon` field; routes match lazy-loaded app routes (Dashboard, Compliance Execution, Action Center, Reports, Audit Management, Knowledge & Documentation, Administration).
- **UI:** `SidebarComponent` (`sidebar.component.ts`) embeds a large inline template + styles. It renders **section titles**, **parent rows** (with optional **expand** for children), **submenu** links, **badges** via `actionBadge()` (routed to mock counts today), and a **quick-actions** strip (Task, Evidence, Finding) that **duplicates** several nav destinations.
- **Collapse:** `collapsed` signal + `localStorage` (`praman-sidebar-collapsed`) narrows the rail to **72px** and hides labels; **MatTooltip** shows labels when collapsed (`matTooltipDisabled` when not collapsed). This is **toggle** collapse, not **hover-expand hybrid**.
- **Mobile:** Sidebar becomes a **fixed drawer** (`max-width: 767px`) with backdrop, **focus trap**, Escape to close; `ShellComponent` sets `pointer-events: none` on main when overlay open. **No bottom navigation** today — footer is static PRAMAN branding only.
- **Tokens / theme:** Sidebar styles already use CSS custom properties (`--color-*`, `--sidebar-bg`, etc.), consistent with the design token stack.

## Gaps vs. stakeholder goals

| Goal | Gap |
|------|-----|
| Icon-primary rows (like quick-actions) | Main nav is still **text-row** forward; quick-actions look more “icon tile” than scrollable list rows. |
| Optional **hover** to show labels (hybrid) | Only **binary** collapsed/expanded; no **hover-flyout** label rail. |
| **Tooltip** + optional long-press on mobile | Tooltips on hover OK; touch **long-press** or **accessible name** strategy not specified in code for icon-only mobile strip. |
| **min 44px** touch targets | Some rows use ~42px in collapsed mode — may need token-based `min-height` ≥ 44px for touch. |
| **Badge** colors (critical / warning) | Variants exist; ensure **Issues** uses **warning**, **Findings** **critical**, **Alerts** **neutral** or product choice. |
| **Ripple** on click | Not wired on nav anchors; Material **MatRipple** or `mat-button` pattern can be added without routing changes. |
| **Dedup** Task / Evidence / Finding | Quick-actions vs. main nav — **single source of truth** from config recommended. |

## Technical options (no backend impact)

1. **Config shape:** Extend `SidebarMenuItem` with optional `badgeKey`, `tooltip`, or **`iconKey`** (map to shared SVG / Lucide/ng-icons) while keeping `route` and hierarchy **unchanged**. Alias type name **`NavItem`** can re-export `SidebarMenuItem` for clarity — avoid branching duplicate models.
2. **Icons:** Prefer **one** approach: keep **inline SVG** in model for zero new deps, **or** align with Journey (Lucide via `ng-icons`) for consistency — product trade-off: bundle size vs. consistency. Plans should default to **refactor-safe** inline or a small **sidebar-icons.ts** map keyed by `id`.
3. **Hybrid hover:** Implement with CSS `:hover` on sidebar host class (e.g. `sidebar-host--compact-hover`) expanding width + showing labels, **or** a **floating label panel** absolutely positioned — prefer token-based width transitions with **prefers-reduced-motion** respected (already partially present).
4. **Mobile bottom nav:** New presentational **`AppBottomNavComponent`** in `core/layout`, driven by a **slice** of the same nav config (Compliance + Action items only, or + Dashboard — **flag in plan**). Routes unchanged; component uses `routerLink` + `RouterLinkActive`. Reserve **safe area** and **z-index** tokens below modals, above content.

## Risks

- **Accessibility:** Icon-only mode requires **`aria-label`** (or visible text off-screen) matching current **tooltip** copy; ensure **keyboard** focus order unchanged.
- **Submenus:** Evidence / Reports / CCC retain **children** — icon-only + nested items need **expanded** panel or **flyout** behavior when parent is icon-only (must_haves in plans).

## RESEARCH COMPLETE

Findings are sufficient to execute Phase 57 plans without additional discovery.
