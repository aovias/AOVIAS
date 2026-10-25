# Phase 61 — Plan 01 Summary — Stage 5A: Enterprise Administration Foundation

**Status:** Complete (implementation + build + route verification).
**human_needed:** rendered-UI signoff (see §6).
**Repository touched:** `WebApp` only. No backend changes, no schema, no migrations.

---

## 1. What shipped

### Workspace tabs

`AdministrationShellComponent` was restructured from a single stacked eight-section aside
into the Stage 5A model:

- **Tab strip** — twelve tabs (Organization, Programs, Templates, Frameworks, Risk,
  Workflow, Notifications, Approvals, Teams, Security, AI, System), horizontally scrollable,
  active tab resolved from the URL by longest-prefix match.
- **Aside** — lists only the active tab's pages, not every page in the workspace.
- **Breadcrumb** — Administration › Tab › Page, in the context bar.
- **Context drawer** — `<app-context-drawer>` rendered in the shell with a `Context` toggle.

The component was also un-double-spaced. It had a blank line between every source line
(790 lines for ~395 lines of code); that was a formatting artefact, not a style, and it made
the file unreadable.

### Shared primitives

New, in `src/app/shared/ui/administration/` and exported from `index.ts`:

- **`AdminToolbarComponent`** — shared toolbar. Title, subtitle, icon, and three projection
  slots: `toolbarSearch`, `toolbarFilters`, `toolbarActions`. The slots are prefixed
  deliberately: a bare `filters` attribute binds `AdminFilterBarComponent.filters` to the
  empty string, which is exactly the compile error the first build produced.
- **`AdminFilterBarComponent`** — declarative filters from `AdminFilterDefinition[]`,
  emitting `AdminFilterChange` with both the individual change and the resulting selection.
  Selection state stays with the caller so filters survive a tab's own reloads.

Reused rather than duplicated: `AdminPageStateComponent`, `AdminSectionCardComponent`,
`AdminStatCardComponent`, `AdminStatusChipComponent`, `WorkspaceSearchComponent`,
`ContextDrawerService` / `ContextDrawerComponent`.

### Navigation model

`settings-nav.model.ts` rewritten around `AdministrationTab`. Route bases are declared once
in `ADMIN_ROUTES` so routes and navigation cannot drift. `getSettingsNavSections()` is kept
as a derived view for `SettingsBreadcrumbService`. `getSettingsHomeCards()` is now **derived
from the tabs** — a tab whose pages are all `comingSoon` reports "Coming Soon" rather than a
hand-maintained "Configured" that nothing verifies.

### Frameworks tab — the one tab bound to live APIs

`FrameworksAdminPageComponent` uses the existing `FrameworksApiService` (catalog + publish /
activate / retire / archive). No parallel client. Search, status filter, status chips,
loading / error / empty / no-results states, and lifecycle actions behind a confirmation
dialog. Status is derived defensively because the catalog reports either an explicit
`status` or only `isActive` + timestamps.

### Consolidation — "no duplicate pages", "no child sidebar menus"

Four features rendered their own in-page nav listing the same items the workspace aside now
shows. Those navs were removed from `AiAdminShellComponent`,
`NotificationAdminShellComponent`, `WorkforceAdminShellComponent`, and
`IntegrationAdminShellComponent`.

Two top-level admin features were folded into the workspace:

- `/app/access-governance/*` → `settings/security/*` (roles, permissions, policies,
  effective, simulator) and `settings/approvals/*` (delegations, matrix). The pre-existing
  `responsibilities` → `/app/governance/responsibilities` hand-off is preserved.
- `/app/security/*` → `settings/security/posture/*`.

`/app/frameworks` and `/app/risks` were **deliberately left operational** — see §4.

### Scaffolds

Programs, Templates, Risk, Workflow, Approvals (chains, signatures), Organization
(facilities, financial year, calendar, localization), and System (number sequences,
retention, feature flags, compliance defaults) are routed and reachable through
`SettingsPlaceholderPageComponent`, each naming its actual status and its Stage 5B contents.

No scaffold shows invented rows. An empty state that says a capability is not yet
configurable is honest; a table of fabricated data is not.

---

## 2. Backward compatibility

Every path that resolved before Stage 5A still resolves. Redirects added:

| Legacy path | Now resolves to |
| --- | --- |
| `settings/general/organization` | `settings/organization/profile` |
| `settings/general/branding` | `settings/organization/branding` |
| `settings/identity/users` | `settings/security/users` |
| `settings/identity/password` | `settings/security/password` |
| `settings/workforce/**` | `settings/teams/**` |
| `settings/platform/integrations/**` | `settings/system/integrations/**` |
| `settings/platform/licensing` | `settings/system/licensing` |
| `settings/platform/commercial` | `settings/system/commercial` |
| `settings/platform/compliance` | `settings/system/compliance-defaults` |
| `settings/platform/notifications/**` | `settings/notifications/**` |
| `/app/access-governance/**` | `settings/security/**`, `settings/approvals/**` |
| `/app/security/**` | `settings/security/posture/**` |

All existing `identity/ai-identity/*` → `ai/*` redirects are unchanged.

**Bug fixed in passing.** The four renamed notification redirects
(`delivery`→`queues`, `reminders`→`subscriptions`, `campaigns`→overview,
`availability`→`preferences`) were previously declared *after* the
`platform/notifications` prefix redirect, so the prefix match claimed them first and sent
`delivery` to a non-existent `notifications/delivery`. They now precede it and work.

Deep links in `training`, `professional`, and `enterprise-directory` pages, plus
`WORKFORCE_ADMIN_BASE` and `INTEGRATION_ADMIN_BASE`, were repointed at the new paths so
routine navigation does not take a redirect hop.

---

## 3. Verification performed

**Build:** `ng build --configuration development` — clean, 2.10 MB initial. The only
warnings are pre-existing and in unrelated features (`oe-ownership-matrix`,
`quality-intelligence`, `commercial-admin`, `trial-status-panel`). Production build was not
attempted; per `57-VERIFICATION.md` it may still fail on a pre-existing SCSS budget unrelated
to this work.

**Routes, at runtime** on a dev server at `localhost:4300`. Angular resolves `redirectTo`
during URL recognition, *before* `authGuard` runs, so the `returnUrl` the guard writes proves
the redirect chain resolved — each case below was checked in the browser:

| Navigated to | `returnUrl` observed |
| --- | --- |
| `/app/settings/general/organization` | `/app/settings/organization/profile` |
| `/app/settings/workforce/departments` | `/app/settings/teams/departments` |
| `/app/settings/platform/notifications/delivery` | `/app/settings/notifications/queues` |
| `/app/settings/platform/integrations/connectors` | `/app/settings/system/integrations/connectors` |
| `/app/settings/identity/users` | `/app/settings/security/users` |
| `/app/settings/approvals` | `/app/settings/approvals/matrix` |

That covers all four redirect mechanisms in use (in-workspace full, in-workspace prefix,
ordered renamed target, tab landing). No console errors on any navigation.

---

## 4. Decisions taken

The three open questions in `61-01-PLAN.md` were resolved as recommended:

1. **Frameworks — split.** `/app/frameworks` (Framework Navigator) stays operational.
   Administration governs lifecycle, not day-to-day browsing; auditors browse frameworks
   constantly and that is not an administrative act.
2. **Risk — split.** `/app/risks` stays operational. The matrix, formula, levels, and
   thresholds are administration and land in Stage 5B.
3. **Governance — left as-is.** `/app/governance` was not in the Stage 5 tab list. Revisit in
   Plan 03 when its configuration consumers are known.

---

## 5. Known debt from this wave

- `ai-admin-nav.component.ts`, `notification-admin-nav.component.ts`,
  `workforce-admin-nav.component.ts`, and `integration-admin-nav.component.ts` are now
  unreferenced. Their **models** are still used (`SettingsBreadcrumbService`,
  `notification-overview-page`), so only the four component files are dead. Left in place
  this wave; delete in Plan 02.
- `SettingsBreadcrumbService` is not consumed by any component — it was already dead before
  this wave. The shell now renders its own breadcrumb. Decide whether to adopt or delete it.
- `SettingsHomeCardStatus` still declares `'configured'`, which the derived card builder no
  longer produces.
- `.claude/launch.json` gained a `praman-web-alt` config on port 4300, because port 4200 was
  held by another session's dev server.

---

## 6. human_needed

Rendered-UI signoff could not be completed. `/app/settings` is behind `authGuard`, and
creating an authenticated session requires entering credentials, which I do not do. Route
resolution and build are verified; what remains unverified by me is visual:

- Tab strip rendering and overflow scrolling at desktop, tablet, and mobile widths.
- Aside showing only the active tab's items, with correct active styling.
- Context drawer open / close / pin from the workspace.
- Frameworks tab against live catalog data, including a lifecycle action end to end.
- Light and dark theme parity on the tab strip and toolbar.
