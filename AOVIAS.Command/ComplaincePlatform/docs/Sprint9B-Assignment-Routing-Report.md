# Sprint 9B — Assignment & Routing Engine — Validation Report

**Date:** 2026-06-23  
**Status:** Complete  
**Predecessor:** [Sprint 9A Readiness Audit](./Sprint9A-Assignment-Routing-Readiness-Audit.md)

---

## Executive Summary

Sprint 9B operationalizes the 503/503 Objective Element department + role metadata discovered in Sprint 9A into an intelligent assignment and routing engine. Primary remediation task ownership is now the write authority; deficiency owner is a synchronized projection. Routing Rules A/B/C are implemented with auto-assign and suggested-assignment UX.

---

## Deliverables Checklist

| # | Deliverable | Status | Notes |
|---|-------------|--------|-------|
| 1 | Ownership Foundation | ✅ | `DeficiencyTaskOwnershipSyncService` — bidirectional task ↔ deficiency sync |
| 2 | Department Normalization | ✅ | `DepartmentLabelCatalog` + `DepartmentLabelMapping` mapping layer |
| 3 | DepartmentOwnership Model | ✅ | Entity, migration, internal + gateway APIs |
| 4 | AssignmentRoutingService | ✅ | `IAssignmentRoutingService` / `AssignmentRoutingService` |
| 5 | Routing Rules A/B/C | ✅ | OE dept → owner; root cause; finding source |
| 6 | Auto Assignment Logic | ✅ | High confidence + configured owner; never blocks task creation |
| 7 | Suggested Assignment UX | ✅ | DCC detail panel + findings promotion preview |
| 8 | Event Integration | ✅ | `TaskReassigned`, `DeficiencyOwnerChanged`, `RoutingRecommendationGenerated` |
| 9 | Analytics Foundation | ✅ | `AssignmentRoutingLog` + `/assignment/metrics` |
| 10 | Validation Report | ✅ | This document |

---

## Phase 0 — Ownership Foundation

**Authority:** Primary remediation task owner is canonical.

| Event | Behavior |
|-------|----------|
| Task reassignment | `SyncDeficiencyOwnerFromTaskAsync` updates deficiency `AssignedTo` |
| Deficiency assign | `SyncTaskOwnerFromDeficiencyAsync` updates primary task owner |
| Routing auto-assign | `SetPrimaryOwnerAsync` writes task first, syncs deficiency |

**Key files:**
- `ComplianceService/Application/Services/DeficiencyTaskOwnershipSyncService.cs`
- `ComplianceService/Application/Contracts/IAssignmentRoutingContracts.cs`

**Recursion guard:** `EnsureRemediationTaskAsync(..., runAutoRouting: false)` when called from ownership sync prevents infinite routing loops.

---

## Phase 1 — Department Normalization

Non-destructive mapping layer — existing free-text OE labels and legacy department fields unchanged.

| OE Label | Normalized Code |
|----------|-----------------|
| Clinical / Nursing | `CLINICAL_NURSING` |
| Pharmacy | `PHARMACY` |
| Facility / Engineering | `FACILITY_ENGINEERING` |
| Quality Management | `QUALITY_MANAGEMENT` |
| Human Resources | `HUMAN_RESOURCES` |
| … | (10 catalog entries seeded) |

**Migration:** `Sprint9B_AssignmentRouting` — `DepartmentLabelMappings`, `DepartmentOwnerships`, `AssignmentRoutingLogs`

---

## Phase 2 — DepartmentOwnership

```
OrganizationId, HospitalId?, DepartmentId, PrimaryOwnerUserId, BackupOwnerUserId,
EffectiveFrom, EffectiveTo?, Status
```

**APIs:**
- Gateway: `GET/PUT /api/v1/assignment/department-ownership`
- Internal: `InternalAssignmentController`

---

## Phase 3 — Organization Member Enhancements

Added to `OrganizationMember`: `Designation`, `ReportsToUserId`.  
DTOs expose `DepartmentId`, `DepartmentName`, `Designation`, `ReportsToUserId` through OrganizationService, Gateway, and WebApp.

**Migration:** `OrganizationService` — `Sprint9B_MemberDesignation`

---

## Phase 4–8 — Routing Engine

### Rule A (P0) — OE Department
```
OE Department Label → Normalized Code → ComplianceDepartment → DepartmentOwnership → Assignee
```
Confidence: **High** when owner configured; **Medium** when department resolved but no owner.

### Rule B (P1) — Root Cause (fallback when Rule A < Medium)
| Root Cause | Department |
|------------|------------|
| TrainingMissing | HUMAN_RESOURCES |
| PolicyMissing / ProcessFailure / … | QUALITY_MANAGEMENT |

Auto-assign: **disabled** (suggestion only).

### Rule C (P2) — Finding Source (lowest priority)
| Source | Department |
|--------|------------|
| InternalAudit / ExternalAudit / ComplianceTeam | QUALITY_MANAGEMENT |
| IncidentReview | MANAGEMENT |
| DepartmentAssessment | Mapped from FindingDepartment |

### Auto Assignment
Applied when `CanAutoAssign == true` (Rule A + configured owner) OR user forces via API.  
Hooked into `DeficiencyRemediationTaskService.EnsureRemediationTaskAsync` after task create/link.

---

## Phase 9 — Suggested Assignment UX

| Surface | Behavior |
|---------|----------|
| DCC detail panel | Shows recommended owner, rule, reason, confidence; Accept button |
| Findings promotion | Pre-promotion routing preview in promotion section |
| API | `GET /assignment/deficiencies/{id}/suggestion`, `POST .../auto-assign` |

User can override via Accept (force when confidence is not High).

---

## Phase 10 — Events (Notification-Ready)

| Event | When |
|-------|------|
| `TaskAssigned` | Auto-routed owner set |
| `TaskReassigned` | Task owner changed via sync |
| `DeficiencyOwnerChanged` | Deficiency projection updated from task |
| `RoutingRecommendationGenerated` | Every routing resolution |

No notification engine in 9B — events persisted for Sprint 10 SLA & Escalation.

---

## Phase 11 — Analytics Foundation

`AssignmentRoutingLog` tracks: rule, confidence, recommended/applied user, auto-assigned flag.

**Metrics endpoint:** `GET /api/v1/assignment/metrics?organizationId=...`

KPIs: auto assignment rate, routing success rate, counts by rule.

---

## Phase 12 — End-to-End Validation

### Automated Tests (181 passing)

| Test Class | Coverage |
|------------|----------|
| `AssignmentRoutingServiceTests` | Rule A high/medium, Rule B fallback, auto-assign apply |
| `DeficiencyRemediationTaskServiceTests` | Task ensure idempotency (with routing no-op) |
| Existing suite | No regressions |

### Manual Validation Path

```
Finding → Promote → Deficiency → EnsureRemediationTask → Routing → Auto/Suggested Assign
  → Evidence → Verification
```

**Pre-requisites for auto-assign in production:**
1. Apply migrations: `Sprint9B_AssignmentRouting`, `Sprint9B_MemberDesignation`
2. Configure `DepartmentOwnership` per org/hospital via PUT `/assignment/department-ownership`

---

## Architecture Constraints Honored

- ✅ No redesign of Tasks, DCC, Journey, or Findings
- ✅ Event-driven patterns maintained
- ✅ Existing remediation workflow preserved
- ✅ Circular DI between routing and remediation resolved (ownership sync uses `runAutoRouting: false`)

---

## Success Criteria

| Criterion | Met |
|-----------|-----|
| Task ownership = source of truth | ✅ |
| Department mappings normalized | ✅ |
| 503 OE mappings utilized | ✅ |
| Intelligent routing operational | ✅ |
| Auto + suggested assignment | ✅ |
| Notification-ready events | ✅ |
| Escalation-ready ownership model | ✅ |
| Analytics-ready tracking | ✅ |
| Ready for Sprint 10 | ✅ |

---

## Known Gaps / Sprint 10 Prep

1. **Department ownership seeding** — org admins must configure owners; no bulk seed from OE metadata yet.
2. **DCC dashboard KPIs** — routing metrics not yet on dashboard (API available).
3. **User display names** — UX shows assignee user IDs; resolve via org member lookup in Sprint 10.
4. **Manual override tracking** — `WasOverridden` flag on logs; wire when explicit override UX added.

---

## Apply Migrations

```bash
dotnet ef database update --project ComplaincePlatform/ComplianceService
dotnet ef database update --project ComplaincePlatform/OrganizationService
```
