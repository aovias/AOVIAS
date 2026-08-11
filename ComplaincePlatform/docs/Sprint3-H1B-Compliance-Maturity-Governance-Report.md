# PRAMAN Sprint 3 — H1B Compliance Maturity Responsibility Cleanup

**Date:** 2026-06-23  
**Scope:** Compliance Maturity only — Journey untouched  
**Goal:** Transform Compliance Maturity into the **Accreditation Governance Workspace**

---

## Executive Summary

Compliance Maturity is now governance-focused: stage blockers, unlock audit history, accreditation forecast, predicted gaps, and navigation to execution modules. Execution controls (assign, upload, edit) were removed from CM surfaces and replaced with **Open Journey / Tasks / Evidence / Issues & Risks** links.

---

# Deliverable 1 — Compliance Maturity Audit

| Widget | Classification | Action (Sprint 3) |
|--------|----------------|-------------------|
| CMI Header | Governance | Renamed to Accreditation Governance Workspace |
| Executive KPIs | Analytics / Governance | Kept — links to Journey for remediation |
| Stage Governance Timeline | Governance | Enhanced — `blockingReasons` from API |
| Accreditation Readiness Forecast | Forecast | **New component** |
| Stage Blockers panel | Governance | **New component** |
| Compliance Deficiencies panel | Governance (read-only) | Replaced disabled stubs with nav links |
| Predicted Gaps ×3 | Prediction | Renamed; advisory badges retained |
| Unlock Audit History | Governance | **New component** |
| Chapter Heatmap | Analytics | Kept |
| Journey Sidebar | Governance | Kept |
| Governance Recommendations | Governance | Renamed from Recommended Actions |
| Governance Navigation | Navigation | Replaced non-functional Quick Actions |
| Orphan: maturity-stepper, *-compliance ×3 | Legacy | Unchanged (not routed) |

**Removed from CM:** Disabled View/Assign/Evidence execution stubs, non-wired Quick Action buttons, client hardcoded unlock thresholds (70/75/85) in timeline.

---

# Deliverable 2 — Ownership Matrix

| Responsibility | Compliance Maturity | Journey |
|----------------|:-------------------:|:-------:|
| CMI | ✓ | — |
| Stage unlock criteria / blockers | ✓ | consume only |
| Unlock audit history | ✓ | — |
| Predicted gaps | ✓ | — |
| Accreditation forecast | ✓ | — |
| Deficiency editing | — | ✓ |
| Task assignment | — | ✓ |
| Evidence upload | — | ✓ |
| OE execution | — | ✓ |
| Stage state display | ✓ consume | ✓ consume |
| **Authority:** StageUnlockingService | ✓ | ✓ (same mapper) |

---

# Deliverable 3 — Governance Model

Compliance Maturity answers:

| Question | Surface |
|----------|---------|
| Why am I blocked? | Stage Blockers + timeline `blockingReasons` |
| How many obligations remain? | Stage Blockers → Remaining Obligations |
| How many deficiencies prevent progression? | KPIs + deficiency panel + blockers |
| What is required to unlock next stage? | Timeline unlock hints + recommendations |
| How close are we to accreditation? | Accreditation Readiness Forecast + CMI |
| What future gaps should we prepare for? | Predicted Gaps panels |

Execution answers: *What am I working on?* — Journey only.

---

# Deliverable 4 — Blocking Reason Framework

**Backend:** `StageBlockingSummaryDto` on dashboard (`activeStageBlocking`)

| Field | Source |
|-------|--------|
| Open Critical Deficiencies | `StageUnlockMetricsSnapshot` / core summary |
| Missing Evidence | Obligation projection metrics |
| Verification Failures | Pending verification count |
| Readiness Gap | `unlockThreshold - readinessPercent` |
| Remaining Obligations | Total − verified obligations |
| Blocking Reasons | `StageUnlockingService` → `BlockingReasons` |

**Frontend:** `CmStageBlockingComponent` + timeline blocker lists per stage.

---

# Deliverable 5 — Predicted Gap Framework

- Three panels: **Predicted Gaps — Commitment / Achievement / Excellence**
- `ComplianceClassificationBadge` → `predictedGap` (Sprint 1 H8)
- Subtitle clarifies: *Advisory — not deficiencies*
- Backend: `PredictedGapEngineService` (deterministic rules, no AI)

---

# Deliverable 6 — Unlock Audit Timeline

**Backend:** `UnlockAuditTimelineEntryDto[]` from `MaturityStageUnlockAuditEntry` (existing domain, no new entity)

Event types mapped: `StageCompleted`, `StageUnlocked`, `StageBlocked`, `StageProgress`

**Frontend:** `CmUnlockAuditTimelineComponent`

---

# Deliverable 7 — Forecasting Layer

**Backend:** `AccreditationForecastDto` (deterministic)

| Field | Derivation |
|-------|------------|
| Current Readiness | Active stage `readinessPercent` |
| Target Readiness | Active stage `unlockThreshold` |
| Estimated Unlock Readiness | `unlockReadinessPercent` / progress |
| Remaining Effort | `100 - estimatedUnlock` |
| CMI | Weighted maturity index |

**Frontend:** `CmGovernanceForecastComponent`

No AI / LLM.

---

# Deliverable 8 — Duplication Audit

| Overlap | Status after Sprint 3 |
|---------|----------------------|
| Stage strip (Journey vs CM) | **Intentional** — Journey = compact context; CM = full governance timeline |
| Deficiency list | CM = governance summary; Journey = execution remediation |
| Predicted gaps | CM only ✓ |
| Quick actions / execution | Removed from CM; Journey owns execution |
| CMI / forecast | CM only ✓ |
| Command Center audit readiness | Program-level — distinct scope |
| Dashboard heatmap | Shared component — CM analytics use |

---

# Deliverable 9 — Validation Report

| Criterion | Result |
|-----------|--------|
| CM = Governance Workspace | ✓ |
| Journey untouched | ✓ No Journey files modified |
| No duplicate execution ownership on CM | ✓ Nav links only |
| StageUnlockingService authority | ✓ Unchanged |
| Predicted Gaps ≠ Deficiencies | ✓ Badges + labels |
| `dotnet test` (146) | ✓ Pass |
| `npm run build` | ✓ Pass |
| Menu unchanged | ✓ |

---

## API Changes (non-breaking)

Extended `ComplianceMaturityDashboardDto`:

- `activeStageBlocking`
- `accreditationForecast`
- `unlockAuditTimeline`

Extended `StageUnlockEvaluationResult.Metrics` for governance metrics (internal unlock pipeline).

---

## Key Files

**Backend:**
- `ComplianceMaturityDtos.cs`
- `ComplianceMaturityAggregationService.cs`
- `StageUnlockCriteriaEvaluator.cs`

**Frontend:**
- `pages/compliance-maturity.component.ts`
- `components/cm-governance-forecast/`
- `components/cm-stage-blocking/`
- `components/cm-unlock-audit-timeline/`
- `components/core-compliance/`
- `components/journey-actions/`
- `components/cm-journey-timeline/`

---

## Ready for Sprint 4 (H3 — Evidence Ownership Consolidation)

CM now links to Evidence module for execution; evidence governance boundaries can be consolidated without CM execution overlap.
