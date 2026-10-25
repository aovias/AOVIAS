# PRAMAN Sprint 7B — Finding → Deficiency Promotion

**Date:** 2026-06-23  
**Scope:** Bridge discovery (Findings) to remediation (Deficiencies) without duplicating workflows  
**Goal:** `Finding (Accepted) → Promote → ComplianceDeficiency → Primary Remediation Task`

---

## Executive Summary

Sprint 7B implements the promotion bridge between PRAMAN's two mature domains. Findings remain discovery-focused; deficiencies remain remediation-focused. One-click promotion creates a deficiency, primary task, bidirectional linkage, audit trail entries, and compliance events.

| Area | Result |
|------|--------|
| **Eligibility** | Classification rules enforced (Good Practice blocked) |
| **Readiness** | Accepted status, OE mapped, obligation resolved, not already promoted |
| **Promotion** | `FindingPromotionService` + `CreateFromFinding` factory |
| **Task** | `Resolve Finding: {FindingNumber}` via `DeficiencyRemediationTaskComposer` |
| **API** | `GET /findings/{id}/promotion-status`, `POST /findings/{id}/promote` |
| **UI** | Findings detail panel + DCC origin section + dashboard KPIs |
| **Migration** | `20260623144137_Sprint7B_FindingPromotionOrigin` applied |

---

# Deliverable 1 — Promotion Eligibility Engine

**Interface:** `IFindingPromotionEligibilityService`  
**Implementation:** `FindingPromotionEligibilityService`

## Classification rules

| Classification | Eligible |
|----------------|----------|
| Audit Finding | ✓ Allowed |
| Observation | ✓ Allowed |
| Non-Conformity | ✓ Allowed |
| Opportunity For Improvement | ✓ Allowed (optional) |
| Good Practice | ✗ Not allowed |

Implemented in `FindingPromotionEligibilityService.IsClassificationEligible()` with validation reasons surfaced via `FindingPromotionEligibilityDto` and `FindingPromotionStatusDto`.

Structural checks (Phase 1):

| Field | Rule |
|-------|------|
| Status | Must be `Accepted` |
| Promoted | `PromotedDeficiencyId` must be null |
| Objective Element | Must be mapped |
| Chapter | Resolved from OE join or `ClauseReference` |
| Standard | Resolved from OE join |
| Department | Must be assigned (non-default enum) |
| Title | Required |

---

# Deliverable 2 — Promotion Readiness Evaluation

Pre-promotion checks (`BuildStatusAsync`):

| Check | Rule |
|-------|------|
| Status | Must be `Accepted` |
| Already promoted | `PromotedDeficiencyId` must be null |
| Classification | Must pass eligibility |
| Objective Element | Must be mapped |
| Obligation | Resolved via journey or hospital + OE |
| Title | Required |
| Hospital context | Required for obligation resolution |

Returns `FindingPromotionStatusDto` with `EligibilityStatus`: `Eligible`, `Not Eligible`, or `Promoted`.

---

# Deliverable 3 — FindingPromotionService

**Interface:** `IFindingPromotionService`  
**Implementation:** `FindingPromotionService`

Responsibilities:

1. Validate finding readiness
2. Resolve `OrganizationComplianceObligation`
3. Create deficiency via `ComplianceDeficiency.CreateFromFinding()`
4. Mark finding promoted via `Finding.MarkPromoted()`
5. Dispatch domain events (outbox)
6. Ensure primary remediation task (`EnsureRemediationTaskAsync`)
7. Log audit trail entries
8. Emit compliance events

---

# Deliverable 4 — Deficiency Creation Mapping

| Finding field | Deficiency field |
|---------------|------------------|
| FindingNumber | OriginReference + deficiency code prefix |
| Title | Title |
| Description | Description |
| Severity | Severity (mapped enum) |
| Chapter/Standard/OE | Via obligation join |
| Department | Assignee context |
| RaisedBy | Traceable via origin finding |
| — | Source = `AuditFinding` |
| Id | OriginId |
| — | OriginType = `AuditFinding` |

**Severity mapping:** Critical→Critical, Major→Major, Moderate→Moderate, Informational/Minor→Minor

**Type mapping:** OFI→Maturity deficiency; others→Assessment deficiency

---

# Deliverable 5 — Primary Remediation Task

Business rule: every promoted deficiency receives exactly one primary task.

- Reuses `DeficiencyRemediationTaskService.EnsureRemediationTaskAsync()` (idempotent)
- Task title: `Resolve Finding: {FindingNumber}` when `OriginType = AuditFinding`
- Links: `SourceDeficiencyId`, OE, hospital, framework

---

# Deliverable 6 — Linkage Model

| Entity | Field | Value |
|--------|-------|-------|
| Finding | `PromotedDeficiencyId` | Created deficiency ID |
| Deficiency | `OriginType` | `AuditFinding` |
| Deficiency | `OriginId` | Finding ID |
| Deficiency | `OriginReference` | Finding number |
| Deficiency | `PromotedAt` | UTC timestamp |

Bidirectional navigation:

- Findings detail → Open Deficiency / Open Remediation Task
- DCC detail → Open Finding (origin section)

---

# Deliverable 7 — Findings UI

**Findings detail panel** (`findings-detail-panel.component.ts`):

- Promotion Status badge (Eligible / Not Eligible / Promoted)
- Validation messages when not ready
- **Promote To Deficiency** button when `allowPromotion`
- **Open Deficiency** / **Open Remediation Task** when promoted
- No remediation controls in Findings

**Dashboard KPIs:** Promoted Findings, Awaiting Promotion, Promotion Rate

---

# Deliverable 8 — DCC Integration

**DCC detail panel** origin section when `originType === 'AuditFinding'`:

- Finding Number
- Finding Source
- Promoted Date
- **Open Finding** action

**DCC dashboard KPIs:** Audit-Origin Deficiencies, Assessment-Origin Deficiencies

---

# Deliverable 9 — Analytics Integration

| Surface | Metrics |
|---------|---------|
| Findings Dashboard | `promotedFindings`, `findingsAwaitingPromotion`, `promotionRate` |
| DCC Dashboard | `auditOriginDeficiencies`, `assessmentOriginDeficiencies` |
| Compliance Maturity | `auditOriginDeficiencyCount` (read-only KPI card) |

---

# Deliverable 10 — Events & API

## Compliance events

| Event | Trigger |
|-------|---------|
| `FindingPromotedToDeficiency` | After successful promotion |
| `DeficiencyCreatedFromFinding` | Deficiency persisted |
| `PrimaryRemediationTaskCreated` | Task provisioned |

## Audit trail actions

- `FindingPromoted`
- `DeficiencyCreated`
- `PrimaryRemediationTaskCreated`

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/findings/{id}/eligibility` | Phase 1 eligibility only (`Eligible` / `NotEligible`) |
| GET | `/api/v1/findings/{id}/promotion-status` | Eligibility + obligation readiness + task links |
| POST | `/api/v1/findings/{id}/promote` | Execute promotion |

Gateway proxy routes added in `FindingsV1ProxyController`.

---

# Schema Changes

Migration `20260623144137_Sprint7B_FindingPromotionOrigin`:

```sql
ALTER TABLE ComplianceDeficiencies ADD
  OriginType int NULL,
  OriginId uniqueidentifier NULL,
  OriginReference nvarchar(64) NULL,
  PromotedAt datetime2 NULL;
```

Enum: `DeficiencyOriginType` (EvaluationEngine, Manual, AuditFinding)

---

# Validation Report

| Step | Status |
|------|--------|
| Finding eligibility enforced | ✓ |
| Readiness evaluation | ✓ |
| Deficiency auto-created | ✓ |
| Primary task auto-created | ✓ |
| Bidirectional traceability | ✓ |
| Audit trail recorded | ✓ |
| Event-driven architecture | ✓ |
| Findings remain discovery-only | ✓ |
| Deficiencies remain remediation-only | ✓ |
| Backend build | ✓ |
| Frontend build | ✓ |
| Migration applied | ✓ |
| Unit tests (eligibility/mapping) | ✓ |

**End-to-end lifecycle** (Finding → Accepted → Promoted → Deficiency → Task → Evidence → Verification → Readiness → Stage Unlock) requires runtime data with mapped OE + obligation; promotion path is wired to existing remediation pipeline.

---

# Success Criteria

✓ Findings remain discovery-focused  
✓ Deficiencies remain remediation-focused  
✓ Promotion eligibility enforced  
✓ One-click promotion available  
✓ Deficiency automatically created  
✓ Primary remediation task automatically created  
✓ Bidirectional traceability  
✓ Audit trail recorded  
✓ Event-driven architecture preserved  
✓ Complete audit-to-remediation lifecycle bridge achieved

---

# Key Files

| Layer | Files |
|-------|-------|
| Domain | `ComplianceDeficiency.cs`, `Finding.cs`, `DeficiencyOriginType.cs` |
| Service | `FindingPromotionEligibilityService.cs`, `FindingPromotionService.cs`, `IFindingPromotionService.cs`, `IFindingPromotionEligibilityService.cs` |
| API | `FindingsController.cs`, `FindingsV1ProxyController.cs` |
| DTOs | `FindingPromotionDtos.cs`, updated workspace/DCC/CM DTOs |
| UI | `findings-detail-panel` (Promote To Deficiency + Open Deficiency), `findings-dashboard` (promotion KPIs), `dcc-detail-panel`, `dcc-dashboard` |
| Migration | `20260623144137_Sprint7B_FindingPromotionOrigin.cs` |
| Tests | `FindingPromotionServiceTests.cs` |
