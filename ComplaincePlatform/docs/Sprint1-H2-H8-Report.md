# PRAMAN Sprint 1 — H2 & H8 Delivery Report

**Date:** 2026-06-23  
**Scope:** Stage unlock display consolidation + gap classification framework

---

## Part 1 — H2 Stage Unlock Display Consolidation

### Stage State Audit (before)

| Location | Source | Issue |
|----------|--------|-------|
| Journey Maturity Dashboard | Client thresholds 70/75/85 | **Duplicate truth** |
| Compliance Maturity Dashboard | `IStageUnlockingService` | Correct |
| Journey `GET …/dashboard` stageBreakdown | OE score aggregation only | No unlock status |
| Dashboard / Command Center | Score services | No stage unlock (unchanged this sprint) |

### Implementation

1. **`MaturityStagePresentationMapper`** — shared mapper from `StageUnlockEvaluationResult` → `MaturityStageDto`
2. **`JourneyDashboardDto`** extended with `currentStage` + `maturityStages` (same DTO shape as Compliance Maturity)
3. **`JourneyWorkspaceAppService.GetDashboardAsync`** calls `IStageUnlockingService.GetStageEvaluationsAsync` and maps via shared mapper
4. **Journey frontend** consumes `maturityStages` from workspace snapshot — **removed all client 70/75/85 logic**
5. **`StageProgressStripComponent`** displays readiness %, status, blocking reasons from server

### Validation

| Surface | Authority |
|---------|-----------|
| Journey Maturity | `GET /journeys/{id}/dashboard` → `maturityStages` |
| Compliance Maturity | `GET /compliance-maturity/dashboard` → `stages` |
| Both | `MaturityStagePresentationMapper` + `StageUnlockingService` |

Legacy server fallback (empty evaluations) uses `StageUnlockCriteriaRegistry` thresholds — **not client-side**.

---

## Part 2 — H8 Gap Classification Framework

### Gap Terminology Audit (before)

| Term | Used in | Meant |
|------|---------|-------|
| Gap / Open Gaps | Compliance Maturity KPIs, Core panel | Mixed (deficiencies + generic) |
| Actual Gaps | Core compliance panel | Compliance deficiencies |
| Predicted Opportunities | Upper-stage panels | Predicted gaps (advisory) |
| Finding | Audit module | Audit findings |
| Corrective Actions | Issues & Risks nav | Mock CAPA (not deficiencies) |

### Standardization

| Classification | Label | Surfaces |
|----------------|-------|----------|
| `auditFinding` | AUDIT FINDING | Findings module rows |
| `complianceDeficiency` | COMPLIANCE DEFICIENCY | Journey deficiencies, CM Core panel, CM KPIs |
| `predictedGap` | PREDICTED GAP | CM predicted opportunity panels |

### DTO Extension (non-breaking)

- `ComplianceItemClassification` enum on backend
- Default values on `GapItemDto`, `PredictedGapItemDto`, `FindingListItemDto`
- Frontend `ComplianceClassificationBadgeComponent` with semantic design tokens (light/dark via CSS variables)

---

## Files Changed (summary)

**Backend:** `MaturityStagePresentationMapper.cs`, `JourneyWorkspaceAppService.cs`, `ComplianceMaturityAggregationService.cs`, DTOs  
**Frontend:** Shared models/badge, Journey maturity dashboard, stage progress strip, CM core/predicted/KPI/timeline, audit findings row, Issues & Risks page title

---

## Success Criteria Checklist

- [x] `StageUnlockingService` is the only stage authority for Journey + Compliance Maturity displays
- [x] No hardcoded unlock thresholds in Journey UI
- [x] Journey and Compliance Maturity consume same mapper/DTO contract
- [x] Audit Findings vs Compliance Deficiencies vs Predicted Gaps visually distinct
- [x] No menu changes, no screen redesigns, Standards & Chapters untouched
- [x] Existing architecture preserved; no new domain entities
