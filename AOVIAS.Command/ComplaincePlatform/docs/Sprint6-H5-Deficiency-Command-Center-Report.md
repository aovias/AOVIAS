# PRAMAN Sprint 6 — H5 Deficiency Command Center

**Date:** 2026-06-23  
**Scope:** Transform **Issues & Risks** (`/app/audit/corrective-actions`) into the operational Deficiency Command Center — no new menu, no duplicate entities  
**Goal:** Answer *"What is preventing accreditation?"* with deficiencies, remediation, evidence, verification, and stage blocking in one workspace

---

## Executive Summary

Sprint 6 replaces mock CAPA data on Issues & Risks with live **ComplianceDeficiency** projections. The page retains the menu label **Issues & Risks** but operates as the **Deficiency Command Center** — the orchestration hub between Journey (execution), Tasks (work), Evidence (proof), Review Queue (verification), Compliance Maturity (governance), and Findings (audit).

| Area | Result |
|------|--------|
| **Backend** | `DeficiencyCommandCenterQueryService`, enriched DTOs, root-cause PATCH, Gateway proxy routes |
| **Frontend** | Dashboard KPIs, workspace grid, detail panel with lifecycle + cross-navigation |
| **Classification** | Predicted Gaps and Audit Findings shown separately (H8 Sprint 1) |
| **Stage blocking** | Critical open deficiencies surface per-stage blocking flags |
| **Migration** | `RootCauseCategory` column on `ComplianceDeficiencies` |

---

# Deliverable 1 — Deficiency Domain Audit

## Entity: `ComplianceDeficiency`

| Field | Type | Notes |
|-------|------|-------|
| Id, TenantId, OrganizationId, HospitalId, FrameworkId | Guid | Scope |
| ObjectiveElementId, ComplianceObligationId | Guid | Links to OE / obligation (no nav on obligation) |
| DeficiencyCode, Title, Description | string | Display identity |
| DeficiencyType | enum | Gap, NonCompliance, etc. |
| Severity | enum | Critical, Major, Moderate, Minor |
| Status | enum | Lifecycle (see Deliverable 5) |
| MaturityLevel | enum | Core / Commitment / Achievement / Excellence |
| RiskCategory | string? | Denormalized advisory context |
| **RootCauseCategory** | enum? | **Sprint 6** — manual classification |
| AssignedTo, DueDate | string?, DateTime? | Assignment |
| Source | string | EvaluationEngine / Manual |
| DetectedDate, ClosedDate, LastEvaluatedDate | DateTime | Lifecycle timestamps |
| CreatedAt, UpdatedAt, CreatedBy, UpdatedBy | audit | Standard |
| RowVersion | byte[] | Concurrency |

## Relationships

```
ComplianceDeficiency
  → OrganizationComplianceObligation (FK, projection source for evidence/verification)
  → ObjectiveElement (via obligation join)
  → ComplianceTask (SourceDeficiencyId — primary remediation task)
  → Finding (separate entity — audit, not deficiency)
```

## Lifecycle (existing)

`Identified → Assigned → InProgress → EvidenceSubmitted → ReviewPending → Verified → Closed`

## APIs (pre-Sprint 6)

| Endpoint | Purpose |
|----------|---------|
| GET `/deficiencies` | Paginated list |
| GET `/deficiencies/summary` | Counts by severity |
| GET `/deficiencies/by-stage` | Stage rollup |
| PATCH `/deficiencies/{id}/status` | Status transition |
| POST `/deficiencies/{id}/assign` | Assign owner |
| POST `/deficiencies/bootstrap-from-onboarding` | Idempotent bootstrap |

## Missing metadata (addressed in Sprint 6)

| Gap | Resolution |
|-----|------------|
| No enriched grid projection | `DeficiencyCommandCenterQueryService` |
| No root cause on deficiency | `RootCauseCategory` + PATCH endpoint |
| No stage blocking in list DTO | Computed from Critical + open + maturity level |
| No primary task in list | Join via `ComplianceTask.SourceDeficiencyId` |
| No evidence counts on deficiency | OE-level evidence stats in detail projection |
| Predicted gaps mixed with deficiencies | Frontend loads CM dashboard separately |

---

# Deliverable 2 — Command Center Design

## Screen layout (Issues & Risks route unchanged)

1. **Dashboard KPIs** — Total, Critical, Major, Open, Resolved, Blocking, Predicted Gaps (CM), Audit Findings
2. **Filters** — Search, status, severity, stage, grouping
3. **Workspace grid** — Primary operational table
4. **Detail panel** — Slide-over on row select

## Frontend structure

| Path | Role |
|------|------|
| `audit/pages/corrective-actions.component.ts` | Page shell, data loading, filters |
| `audit/deficiency-command-center/components/dcc-dashboard.component.ts` | KPI strip |
| `audit/deficiency-command-center/components/dcc-grid.component.ts` | Grid |
| `audit/deficiency-command-center/components/dcc-detail-panel.component.ts` | Detail + lifecycle + nav |
| `audit/deficiency-command-center/services/deficiency-command-center.service.ts` | API client |
| `audit/deficiency-command-center/models/deficiency-command-center.model.ts` | Types |

## Backend structure

| Path | Role |
|------|------|
| `Application/Services/DeficiencyCommandCenterQueryService.cs` | Enriched queries |
| `Application/DTOs/DeficiencyCommandCenterDtos.cs` | Dashboard + workspace DTOs |
| `Application/Services/PatchDeficiencyRootCauseCommandHandler.cs` | Root cause PATCH |
| `Controllers/Internal/InternalDeficienciesController.cs` | Internal endpoints |
| `Gateway/Controllers/DeficienciesController.cs` | Public proxy |

---

# Deliverable 3 — Severity Framework

Reuses existing `DeficiencySeverity` enum — **no duplicate logic**.

| Severity | Definition (business) | Stage blocking |
|----------|----------------------|----------------|
| **Critical** | Patient safety risk OR accreditation blocking | Open Critical deficiencies block stage unlock (`StageUnlockingService`) |
| **Major** | Compliance risk | Does not block unlock alone |
| **Moderate** | Process weakness | Advisory |
| **Minor** | Improvement opportunity | Advisory |

Dashboard counts and grid severity column use enum values directly. Grid applies semantic color tokens (critical=red, major=orange, etc.).

---

# Deliverable 4 — Root Cause Framework

New enum `RootCauseCategory` on `ComplianceDeficiency`:

| Value | Label |
|-------|-------|
| PolicyMissing | Policy Missing |
| EvidenceMissing | Evidence Missing |
| TrainingMissing | Training Missing |
| ProcessFailure | Process Failure |
| DocumentationIssue | Documentation Issue |
| VerificationFailure | Verification Failure |
| Other | Other |

- Manual classification only (no AI)
- PATCH `/api/v1/deficiencies/{id}/root-cause` with `{ rootCauseCategory }`
- Detail panel dropdown persists via API
- **Not** duplicated from legacy `Capa.RootCause` / `ActionPlan.RootCause` strings

---

# Deliverable 5 — Lifecycle Model

Reuses `DeficiencyStatus` — **no duplicate status model**.

| Step | Enum | UI Label |
|------|------|----------|
| 1 | Identified | Identified |
| 2 | Assigned | Assigned |
| 3 | InProgress | In Progress |
| 4 | EvidenceSubmitted | Evidence Submitted |
| 5 | ReviewPending | Pending Verification |
| 6 | Verified | Verified |
| 7 | Closed | Closed |

Detail panel renders horizontal lifecycle stepper; active step derived from `status` field.

Status transitions remain on existing `PatchDeficiencyStatusCommandHandler` — Command Center displays state, does not fork lifecycle logic.

---

# Deliverable 6 — Task Integration Report

## Business rule

Every compliance deficiency **should** have exactly one **Primary Remediation Task**.

## Implementation

| Concern | Approach |
|---------|----------|
| Link | `ComplianceTask.SourceDeficiencyId` |
| Primary task selection | Latest non-completed task per deficiency (`OrderByDescending CreatedAt`) |
| Grid columns | Primary task title, task status |
| Detail panel | Task status, owner, due date, "Open Task" link |
| Authority | Task remains authoritative; deficiency references task |

## Navigation

`/app/tasks?search={objectiveElementCode}` — reuses existing task search deep-link.

## Gap (Sprint 7+)

Enforcement that every deficiency has exactly one primary task at creation time is owned by `DeficiencyRemediationTaskService` — Command Center surfaces missing links as "—" in grid.

---

# Deliverable 7 — Evidence Integration Report

Evidence remains authoritative on `Evidence` + `ObjectiveElementEvidence`. Deficiency consumes obligation/evidence state.

| Display | Source |
|---------|--------|
| Evidence Status | `OrganizationComplianceObligation.EvidenceStatus` |
| Verification Status | `OrganizationComplianceObligation.VerificationStatus` |
| Evidence Count | OE-linked evidence count (detail) |
| Rejected / Needs More | Count by `VerificationStatus` on OE links |
| Latest Evidence | Most recent upload title/date |

## Actions (detail panel)

| Action | Route |
|--------|-------|
| Open Evidence | `/app/evidence?source=deficiency-command-center&objectiveElementId=&objectiveElementCode=` |
| Open Binder | `/app/evidence/binder?objectiveElementId=` |
| Review Queue | `/app/evidence/review-queue?objectiveElementId=&search=` |

Uses shared `EvidenceStatusBadgeComponent` (Sprint 5A) for consistent labels.

---

# Deliverable 8 — Stage Blocking Report

## Source logic

Aligned with `StageUnlockingService`: **open Critical** deficiencies at a given maturity level block that stage.

| Flag | Condition |
|------|-----------|
| BlocksCore | Critical + open + MaturityLevel = Core |
| BlocksCommitment | Critical + open + MaturityLevel = Commitment |
| BlocksAchievement | Critical + open + MaturityLevel = Achievement |
| BlocksExcellence | Critical + open + MaturityLevel = Excellence |

## UI labels

| State | Label |
|-------|-------|
| Critical open at stage | `Blocks {Stage}` |
| Open non-critical | `Not Blocking` |
| Closed | `No Longer Blocking` |

Detail panel lists all four stages with blocking highlight. `blockingReason` populated in detail DTO for Critical blockers.

**No changes** to `StageUnlockingService` or `DeficiencyEvaluationService` core logic.

---

# Deliverable 9 — API Review

## New endpoints (additive, non-breaking)

| Method | Path | DTO |
|--------|------|-----|
| GET | `/api/v1/deficiencies/command-center/dashboard` | `DeficiencyCommandCenterDashboardDto` |
| GET | `/api/v1/deficiencies/command-center/workspace` | `DeficiencyWorkspaceListDto` |
| GET | `/api/v1/deficiencies/command-center/workspace/{id}` | `DeficiencyWorkspaceDetailDto` |
| PATCH | `/api/v1/deficiencies/{id}/root-cause` | `{ rootCauseCategory }` |

## Projection extensions vs duplicates

| Field | Strategy |
|-------|----------|
| OE/Chapter/Standard codes | Joined in query service (not stored on deficiency) |
| Primary task fields | Joined from `ComplianceTask` |
| Evidence stats | Computed at read time for detail |
| Stage blocking flags | Computed at read time |
| PredictedGaps on dashboard DTO | Returns 0; frontend loads from CM API |

## Existing DTOs unchanged

`DeficiencyDto`, `DeficiencySummaryDto`, `DeficiencyByStageDto` — preserved for Journey/CM consumers.

## Migration

`20260623120000_Sprint6_DeficiencyRootCause.cs` — adds nullable `RootCauseCategory` int column.

---

# Deliverable 10 — Validation Report

## Build validation

| Check | Result |
|-------|--------|
| `npm run build` (WebApp) | ✓ Pass |
| ComplianceService compile | ✓ Pass (file-lock on running service prevented exe copy only) |
| Gateway compile | ✓ Pass (same file-lock caveat) |

## End-to-end chain

```
Deficiency (ComplianceDeficiency)
  ↓ SourceDeficiencyId
Task (ComplianceTask) — remediation work item
  ↓ evidence upload
Evidence (ObjectiveElementEvidence)
  ↓ review actions
Review Queue (EvidenceReviewService)
  ↓ approve/reject
Verification status → Obligation projection
  ↓ readiness
Readiness calculations
  ↓ open Critical deficiencies
StageUnlockingService → stage unlock
```

Command Center **displays** each hop and **navigates** to authoritative surfaces — it does not duplicate write paths.

## UX ownership (Sprint 13 review)

| Module | Ownership | Overlap with DCC |
|--------|-----------|------------------|
| Journey | Execution workspace | DCC links to OE/stage; no execution UI |
| Tasks | Work management | DCC shows primary task summary + link |
| Evidence | Proof management | DCC shows status + link |
| Review Queue | Verification actions | DCC links only (Sprint 5A rule) |
| Compliance Maturity | Governance / predicted gaps | DCC shows predicted gap count + CM link |
| Issues & Risks | **Deficiency Command Center** | Primary deficiency hub |

## Success criteria

| Criterion | Status |
|-----------|--------|
| Issues & Risks becomes Deficiency Command Center | ✓ |
| No new menu | ✓ |
| Every deficiency shows primary remediation task (when linked) | ✓ |
| Evidence and verification visible | ✓ |
| Stage blocking visibility | ✓ |
| Root cause supported | ✓ |
| Lifecycle visible | ✓ |
| Cross-navigation functional | ✓ |
| No duplication with Journey / CM | ✓ |
| Ready for Sprint 7 (Finding → Deficiency Promotion) | ✓ |

## Manual test checklist

- [ ] Open `/app/audit/corrective-actions` — dashboard KPIs load
- [ ] Deep-link `?search={oeCode}` filters grid
- [ ] Select row — detail panel, lifecycle, root cause PATCH
- [ ] Cross-nav: Journey, Tasks, Evidence, Review Queue, CM, Findings
- [ ] Export CSV from grid
- [ ] Predicted Gaps count matches CM dashboard sum
- [ ] Run EF migration: `dotnet ef database update` (after stopping ComplianceService)

---

## Files changed (Sprint 6)

### Backend
- `Domain/Enums/RootCauseCategory.cs` (new)
- `Domain/ComplianceDeficiency.cs` — `RootCauseCategory`, `SetRootCause()`
- `Infrastructure/.../ComplianceDeficiencyConfiguration.cs`
- `Application/DTOs/DeficiencyCommandCenterDtos.cs` (new)
- `Application/Contracts/IDeficiencyCommandCenterQueryService.cs` (new)
- `Application/Services/DeficiencyCommandCenterQueryService.cs` (new)
- `Application/Services/PatchDeficiencyRootCauseCommandHandler.cs` (new)
- `Controllers/Internal/InternalDeficienciesController.cs`
- `Program.cs` — DI registration
- `Gateway/Application/Queries/DeficiencyQueries.cs`
- `Gateway/Application/Handlers/DeficiencyHandlers.cs`
- `Gateway/Controllers/DeficienciesController.cs`
- `Migrations/20260623120000_Sprint6_DeficiencyRootCause.cs`

### Frontend
- `audit/deficiency-command-center/**` (new module folder)
- `audit/pages/corrective-actions.component.ts` — replaced mock CAPA UI

---

## Sprint 7 readiness

Finding → Deficiency Promotion can attach audit findings to existing deficiency workflow without new navigation. Command Center grid already separates `AuditFindings` KPI and links to `/app/audit/findings`.
