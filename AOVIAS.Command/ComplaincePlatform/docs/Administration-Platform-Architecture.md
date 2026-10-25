# Administration Platform — Architecture Summary

**Status:** Production-ready consolidation (M7 Administration Platform)  
**Date:** 2026-07-08  
**Scope:** UI/UX consistency, shared components, navigation, permissions reuse, builds — no new business features.

---

## 1. Architecture decisions

- Administration is a **single Angular workspace** under `/app/settings`, hosted by `AdministrationShellComponent`.
- The shell reuses the **shared portal header** (`HeaderComponent` with `administrationMode`), logo, notification bell, theme toggle, and user menu — no duplicated chrome.
- **Entry point:** Administration is opened from the **user profile dropdown** (`Administration` → `/app/settings`), not from the operational left sidebar.
- Domain ownership remains in existing backend services; Administration is a **presentation + orchestration** layer via Gateway APIs.
- Shared Administration UI lives in `WebApp/src/app/shared/ui/administration/` and is wrapped by legacy feature selectors for compatibility.

---

## 2. Navigation map

```
Administration (/app/settings)
├── General
│   ├── Organization          /app/settings/general/organization
│   └── Branding              /app/settings/general/branding
├── Identity & Access
│   ├── Users                 /app/settings/identity/users
│   └── Password Management   /app/settings/identity/password
├── AI
│   ├── Dashboard             /app/settings/ai/dashboard
│   ├── AI Identity           /app/settings/ai/identity
│   ├── Models                /app/settings/ai/models
│   ├── Providers             /app/settings/ai/providers
│   ├── Prompt Library        /app/settings/ai/prompts
│   ├── Policies              /app/settings/ai/policies
│   ├── Executions            /app/settings/ai/executions
│   ├── Usage & Analytics     /app/settings/ai/analytics
│   └── Approvals             /app/settings/ai/approvals
├── Workforce
│   ├── Dashboard             /app/settings/workforce/dashboard
│   ├── Workforce Intelligence /app/settings/workforce/intelligence
│   ├── Enterprise Directory  /app/settings/workforce/directory
│   ├── Departments           /app/settings/workforce/departments
│   ├── Designations          /app/settings/workforce/designations
│   ├── Professional Registry /app/settings/workforce/professionals
│   └── Membership            /app/settings/workforce/memberships
├── Notifications             /app/settings/notifications/**
├── Platform
│   └── Enterprise Integrations /app/settings/platform/integrations/**
├── System
│   └── Audit & Activity      /app/settings/system/audit
└── About                     /app/settings/about
```

Legacy redirects (non-exhaustive): `/app/ai-identity/*`, `/app/workforce-intelligence/*`, `/app/directory/*`, `/app/organization/*`, `/app/professional` (list), `/app/notifications/*`, `/app/integration/*`.

---

## 3. Module ownership

| Admin area | Frontend feature | Backend owner(s) | Gateway surface |
|------------|------------------|------------------|-----------------|
| Organization / Branding | `settings` | OrganizationService | `/settings/organization*` |
| Users / Password | `settings/identity-admin` | IdentityService | Identity admin APIs |
| AI Administration | `ai-identity` | AIOrchestratorService | `/ai/*` |
| Workforce Admin | `workforce-admin` + WI + Directory | OrganizationService, WorkforceIntelligenceService, EnterpriseDirectory, ProfessionalService | `/membership`, `/departments`, `/intelligence`, `/directory`, `/professional-profiles` |
| Notifications | `notifications` | CommunicationService | Notification admin APIs |
| Integrations | `integration` | IntegrationService | Integration APIs |
| Audit | `admin-audit` | Aggregated via Gateway | `/administration/audit/activity` |

---

## 4. Reuse matrix

| Concern | Shared / reused | Not duplicated |
|---------|-----------------|----------------|
| Portal header | `HeaderComponent` | Feature shells only add secondary tabs |
| Page header | `SettingsPageHeaderComponent` | Custom H1 replaced on Org/Users/WI stubs |
| Page state | `AdminPageStateComponent` | AI/WF/Notification wrappers delegate to it |
| Stat cards | `AdminStatCardComponent` | AI/WF/Notification wrappers delegate to it |
| Status chips | `AdminStatusChipComponent` (+ feature-specific where needed) | — |
| Layout CSS | `admin-layout.css` | Ready for table/toolbar adoption |
| Auth | Existing `permissionGuard` + `HasPermissionDirective` | No new permission codes in this sprint |
| Audit | Existing AI/Org/Identity/Integration audit | No new audit framework |
| Storage / branding | Existing org logo + branding assets APIs | — |

---

## 5. Shared component inventory

**New (consolidation)**

| Component | Path | Purpose |
|-----------|------|---------|
| `AdminPageStateComponent` | `shared/ui/administration/admin-page-state.component.ts` | loading / empty / error / permission-denied / no-results |
| `AdminStatCardComponent` | `shared/ui/administration/admin-stat-card.component.ts` | KPI cards |
| `AdminStatusChipComponent` | `shared/ui/administration/admin-status-chip.component.ts` | tone-aware chips |
| `AdminSectionCardComponent` | `shared/ui/administration/admin-section-card.component.ts` | overview capability cards |
| `admin-layout.css` | `shared/ui/administration/admin-layout.css` | tables, toolbars, spacing |

**Existing (retained)**

- `SettingsPageHeaderComponent`, `SettingsComingSoonBadgeComponent`
- Feature shells/navs: AI, Workforce, Notifications, Integration
- `NotificationPageStateComponent` / `AiAdminPageStateComponent` / `WorkforceAdminPageStateComponent` — thin wrappers

---

## 6. Route inventory (primary)

See Navigation map above. Feature entry points:

- `features/settings/routes.ts` — Administration root + General/Identity/System/About
- `features/ai-identity/routes.ts` — AI shell
- `features/workforce-admin/routes.ts` — Workforce shell + nested WI/Directory children
- `features/notifications/routes.ts`
- `features/integration/routes.ts`
- `features/admin-audit` via system/audit path

---

## 7. Permission inventory (Administration-relevant)

| Area | Examples |
|------|----------|
| Identity | `Users.View`, `Users.Create`, `PasswordManagement.Manage` |
| AI | `AI.Profile.Manage`, `AI.Prompt.Manage`, `AI.Policy.Manage`, `AI.Analytics.View` |
| Directory | `Directory.Search.Read`, `Directory.Profile.Read`, `Directory.SavedSearch.Manage`, `Directory.History.Read` |
| Intelligence | `Intelligence.Dashboard.Read`, `Intelligence.Forecast.Read`, `Intelligence.Recommendations.Read`, `Intelligence.Search.Read` |
| Notifications | `Notification.View`, `Notification.Template.Manage`, `Notification.Provider.Manage`, `Notification.Queue.View`, `Notification.History.View`, `Notification.Preference.Manage` |
| Audit | `Audit.View` (and related AG seeds) |
| Branding / Org settings | Org admin roles via Gateway (edit gated by `canEdit`) |

Mutating actions remain hidden via `*hasPermission` / `canEdit` / disabled placeholders where backend manage APIs are not exposed yet (e.g. model Configure/Enable).

---

## 8. Future extension points

- Promote feature shells/nav into a generic `AdminSubNav` / `AdminFeatureShell`
- Migrate remaining page-level table CSS to `admin-layout.css`
- Wire unused `SettingsBreadcrumbService` or remove after nav breadcrumbs stabilize
- Remove dead `settings-password-page` if still unused
- Dedicated Designation catalog API (today: derived from membership + facets)
- Models/Providers registry APIs (today: telemetry-derived views)

---

## 9. Recommendations before M8 – Framework Enrollment

1. Treat Administration as stable chrome; put Framework Enrollment under Compliance operational flows or a new Administration → Compliance Defaults area without duplicating journey ownership.
2. Continue using `ComplianceJourney` + lifecycle state machine — do not invent parallel enrollment statuses in Administration.
3. Prefer Gateway + MediatR CQRS for any enrollment admin APIs; keep controllers thin.
4. Reuse `AdminPageState` / `AdminStatCard` for any new Administration screens.
5. Ensure enrollment permissions are seeded in Access Governance before UI gates are added.
6. Keep Angular feature lazy-loaded; avoid growing the initial shell bundle.
