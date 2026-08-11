# Phase 4 — Enterprise Risk Register (Sprint 4)

**Sprint:** 4  
**Status:** Delivered  
**Principle:** BUILD register APIs/UI on existing `ComplianceService.Domain.Risk` (CRP-048) — do **not** introduce a parallel risk entity.

## Model (SSOT)

Enterprise risk lives on `ComplianceService/Domain/Risk.cs`:

| Field | Purpose |
|-------|---------|
| `TenantId` / `HospitalId` | Org + hospital scope |
| `Category` | Clinical / Operational / Financial / Regulatory (free text) |
| `OwnerUserId` / `OwnerName` | Risk owner |
| `Likelihood` / `Impact` | Inherent score dimensions (1–5); `RiskScore = L × I` (computed) |
| `ResidualLikelihood` / `ResidualImpact` | Residual score after mitigation |
| `MitigationPlan` | Planned / in-progress controls |
| `ReviewDueDate` | Drives Hangfire review-due job |
| `Status` | Lifecycle SSOT (`RiskStatus`) |
| `DepartmentId` | Optional department link |
| `FindingId` / `IncidentId` / `DeficiencyId` / `ObjectiveElementId` / `TaskId` | Cross-domain links |

### Lifecycle

```text
Identified → Assessed → Mitigating → UnderReview → Closed
Identified / Assessed / Mitigating → UnderReview (review-due automation)
Closed is terminal (immutable)
```

Transitions enforced via `RiskStatusExtensions.CanTransitionTo` / `Risk.TransitionTo`. Mutating commands call `EnsureMutable()` so **Closed** risks cannot be edited.

Legacy remap (migration): `Open→Identified`, `Mitigated→Mitigating`, `Closed(3)→Closed(5)`.

## APIs

### ComplianceService — `RisksController`

| Method | Route |
|--------|--------|
| GET | `/api/v1/risks` — list/filter (status, category, search, reviewDueOnly) |
| GET | `/api/v1/risks/{id}` |
| POST | `/api/v1/risks` |
| PUT/PATCH | `/api/v1/risks/{id}` |
| GET | `/api/v1/risks/matrix` — 5×5 heatmap (`useResidual` optional) |

Gateway `RisksV1ProxyController` proxies the same paths via `IProxyService`.

Application service: `IRiskRegisterAppService` / `RiskRegisterAppService`.

## Background jobs (Hangfire)

| Job ID | Schedule (UTC) | Behavior |
|--------|----------------|----------|
| `risk-review-due` | Daily 08:00 | Active risks with `ReviewDueDate <= UtcNow` → `UnderReview`; notify `RiskReviewDue` via `IComplianceNotificationPublisher` |

## Graph

`EnterpriseComplianceGraphService`:

- Search group **Risks**
- Resolve entity type `Risk` → route `/app/risks/register?selected={id}`
- Edges: Finding / Deficiency / Incident / ObjectiveElement / Task
- Incidents also emit outgoing `LinkedTo` Risk edges when `IncidentId` matches

## WebApp (`features/risks`)

| Route | Page |
|-------|------|
| `/app/risks/register` | Register list |
| `/app/risks/new` | Create |
| `/app/risks/:id/edit` | Edit (blocked when Closed) |
| `/app/risks/matrix` | Likelihood × Impact heatmap |

Nav: Action Center **Risk Register**; header mega-menu **Risks**.

### Clarification vs chapter risk

Control Center widget is **Chapter & Department Risk** (compliance readiness heat by chapter/department). It is **not** the Enterprise Risk Register; the widget links to `/app/risks/register`. Sidebar **Deficiencies / CAPA** remains separate from **Risk Register**.

## Migration

`20260726190000_Phase4_EnterpriseRiskRegister`:

- Remaps status integers
- Adds register columns + residual check constraints
- Drops `CK_Risk_ReferenceRequired` (standalone hospital risks allowed)
- FKs to Incidents / ComplianceDeficiencies

## Tests

`ComplianceService.Tests/RiskRegisterTests`:

- Score = Likelihood × Impact; residual score when both set
- Closed risks are immutable
- Matrix excludes Closed
- ReviewDue job → UnderReview + notification

## Exit criteria

Hospital can maintain an enterprise risk register with CRUD, L×I matrix, residual scoring, review-due automation, graph linkage, and clear UI separation from chapter compliance risk.
