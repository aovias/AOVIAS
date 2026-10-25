# Sprint 12B-0 — Analytics Stabilization Report

**Sprint:** 12B-0 Analytics Stabilization  
**Predecessor:** [Sprint 12A Analytics Readiness Audit](./Sprint12A-Analytics-Readiness-Audit.md)  
**Successor:** Sprint 12B-1 Analytics Platform Foundation  
**Date:** June 2026  
**Scope:** KPI SSOT, event reliability, projection refresh, metrics delegation — **no Analytics Hub, charts, or reports built**

---

## Executive Summary

Sprint 12A identified that PRAMAN has sufficient data but **inconsistent KPI calculations** across DCC, Compliance Maturity, Journey, and SLA/CAPA modules. Building Analytics Hub on that foundation would amplify divergence.

Sprint 12B-0 establishes:

1. **Canonical metric definitions** (`Application/Analytics/`)
2. **Unified metrics services** (`IDeficiencyMetricsService`, `IAnalyticsAggregationService`)
3. **Standardized deficiency lifecycle events** (ComplianceEvent + outbox + snapshot refresh)
4. **DCC and CM refactors** to delegate KPI computation
5. **Distinct journey trend series** (no longer three copies of `OverallScore`)
6. **Organization-scoped operational KPIs** (audit findings fixed in DCC)

**Build status:** `ComplianceService` builds clean; **188/188 tests pass**.

---

# Deliverable 1 — Event Reliability Audit

## Required Events — Status

| Event | ComplianceEvent | Outbox Domain Event | Snapshot Refresh | Handler / Source |
|-------|-----------------|---------------------|------------------|------------------|
| `DeficiencyCreated` | ✅ | ✅ `DeficiencyIdentifiedDomainEvent` | ✅ | `DeficiencyRemediationTaskService`, `FindingPromotionService`, `DeficiencyComplianceEventService` |
| `DeficiencyUpdated` | ✅ | ✅ `DeficiencyStatusChangedDomainEvent` | ✅ | `PatchDeficiencyStatusCommandHandler`, domain `TransitionTo` |
| `DeficiencyClosed` | ✅ | ✅ `DeficiencyClosedDomainEvent` | ✅ | Status handler + domain `Close()` |
| `DeficiencyCancelled` | ✅ | ✅ `DeficiencyStatusChangedDomainEvent` | ✅ | `ComplianceDeficiency.Cancel()` (fixed — was silent) |
| `DeficiencyAssigned` | ✅ | — | — | `AssignDeficiencyCommandHandler` |
| `DeficiencyVerified` | ✅ | ✅ status change | ✅ | Status handler |
| `CorrectiveActionUpdated` | ✅ | — | — | `PatchDeficiencyCorrectiveActionCommandHandler` |
| `PreventiveActionUpdated` | ✅ | — | — | `PatchDeficiencyPreventiveActionCommandHandler` |
| `EffectivenessReviewUpdated` | ✅ | — | — | `PatchDeficiencyEffectivenessReviewCommandHandler` |
| `SlaStatusChanged` | ✅ (type added) | — | — | **Partial:** legacy `DeficiencySlaWarning/AtRisk/Breached` still emitted by `SlaEvaluationService`; unified `SlaStatusChanged` available via `IDeficiencyComplianceEventService` |
| `EscalationTriggered` | ✅ as `DeficiencyEscalated` | — | — | `EscalationService` |
| `EscalationAcknowledged` | ✅ | — | — | `EscalationService` |
| `EscalationResolved` | ✅ | — | — | `PatchDeficiencyStatusCommandHandler` on close with active escalation |

## Fixes Applied (Phase 1)

| Gap | Fix |
|-----|-----|
| Deficiency domain events not dispatched to outbox | `OutboxDomainEventDispatcher` now handles `DeficiencyIdentifiedDomainEvent`, `DeficiencyClosedDomainEvent`, `DeficiencyStatusChangedDomainEvent` |
| Cancel did not raise domain event | `ComplianceDeficiency.Cancel()` raises `DeficiencyStatusChangedDomainEvent` |
| Outbox processor ignored deficiency status changes | `InProcessOutboxProcessor` queues snapshot recalc for all three deficiency event types |
| Missing standardized ComplianceEvent types | Added enum values 31–39 in `ComplianceEventType` |
| CAPA patches silent | Preventive, corrective, effectiveness handlers emit via `IDeficiencyComplianceEventService` |
| Assign silent | `AssignDeficiencyCommandHandler` emits `DeficiencyAssigned` |

## Event Matrix (ComplianceEvent → Projection)

| ComplianceEvent | Primary Projection Impact |
|-----------------|----------------------------|
| Deficiency lifecycle (Created/Updated/Closed/Cancelled/Verified) | `ComplianceSnapshots` (via outbox → recalc queue), obligation projection triggers |
| CAPA updates | ComplianceEvent log; live CAPA metrics (no historical fact table yet) |
| SLA warning/at-risk/breached | ComplianceEvent log; live SLA metrics |
| Escalation (triggered/acknowledged/resolved) | ComplianceEvent log; live escalation metrics |
| FindingCreated / FindingPromoted | Findings counts; promotion → deficiency pipeline |
| StageUnlocked / ReadinessThresholdAchieved | Maturity stage state; snapshot recalc |

---

# Deliverable 2 — Projection Dependency Matrix

| Projection | Owner Service | Refresh Triggers | Consumers |
|------------|---------------|------------------|-----------|
| `ComplianceSnapshots` | `ComplianceFrameworkSnapshotRecalculationService` | OE score changes, evidence approval, deficiency outbox events, manual queue, background job | Journey trends, Program Overview, Reports, Control Center |
| `ProgramOverviewSnapshot` | `ProgramOverviewSnapshotReadService` | Delegates to snapshot recalc queue | Executive dashboard |
| `OrganizationComplianceObligations` | `ObligationProjectionService` | Assessment, evidence, deficiency closed, verification | Stage Unlocking, CM obligation bands |
| Maturity stage states | `StageUnlockingService` | Obligation readiness, critical deficiency blockers | CM, Journey maturity stepper |
| CAPA metrics (live) | `CapaQueryService` | Live query on `ComplianceDeficiencies` | DCC, CM (via aggregation), future Analytics Hub |
| SLA/Escalation metrics (live) | `EscalationService` | Live query + escalation history | DCC, CM (via aggregation), future Analytics Hub |
| DCC dashboard DTO | `DeficiencyCommandCenterQueryService` | On read — delegates to metrics services | DCC UI |

## Refresh Path Verification

```
State Change
  → Domain Event (where applicable)
  → OutboxDomainEventDispatcher
  → InProcessOutboxProcessor
  → IComplianceSnapshotRecalculationQueue.EnqueueHospitalFramework
  → ComplianceFrameworkSnapshotRecalculationService
  → ComplianceSnapshots append
```

**Known gap:** CAPA/SLA ComplianceEvents do not directly enqueue snapshot recalc (by design — live metrics). Historical CAPA/SLA trends remain a 12B-1 concern.

---

# Deliverable 3 — Deficiency Metric Definitions (SSOT)

**Location:** `ComplianceService/Application/Analytics/DeficiencyMetricDefinitions.cs`

| Metric | Canonical Definition |
|--------|---------------------|
| **Open Deficiency** | `Status ∉ { Closed, Cancelled }` |
| **Closed Deficiency** | `Status == Closed` |
| **Cancelled Deficiency** | `Status == Cancelled` |
| **Verified Deficiency** | `Status ∈ { Verified, Closed }` |
| **Recurring Deficiency** | `IsRecurringIssue == true` |
| **Escalated Deficiency** | `CurrentEscalationState ∈ { Escalated, Acknowledged }` |
| **SLA-Tracked Open** | Open + `SlaPolicyId != null` |
| **Open CAPA** | Corrective/preventive work started or summary present |

**Consumers refactored:** `DeficiencyCommandCenterQueryService`, `ComplianceMaturityAggregationService`, `EscalationService`, `DeficiencyMetricsService`.

---

# Deliverable 4 — Critical Severity Definitions (SSOT)

**Location:** `ComplianceService/Application/Analytics/CriticalSeverityDefinitions.cs`

| Term | Meaning | Source |
|------|---------|--------|
| **Critical Deficiency** | Operational record with `Severity == Critical` | `ComplianceDeficiencies` |
| **Critical Risk** | Risk matrix `AssessedRiskLevel == Critical` | Deficiency assessment |
| **Critical OE Gap / NC** | Scoring engine NC count | `ComplianceScoreResult.CriticalCount` — **not** deficiency table |
| **Critical Finding** | Discovery record `Severity == Critical`, org-scoped | `Findings` |
| **Open Critical (Unlock)** | Open + Critical severity | Stage unlock blocking |

**Alignment:** DCC operational KPIs use `IsCriticalDeficiency`. CM `GapSummary.CriticalCount` remains OE scoring NC count — documented distinction prevents conflation.

---

# Deliverable 5 — Readiness SSOT Decision

**Location:** `ComplianceService/Application/Analytics/ReadinessMetricDefinitions.cs`

| Readiness Type | Authoritative Source | Use Case |
|----------------|---------------------|----------|
| **Journey Accreditation Readiness** | `IComplianceScoreService.GetJourneyScoreAsync` | Journey dashboard headline, executive accreditation % |
| **Framework Assessment Readiness** | `IComplianceScoringService.CalculateFrameworkScoreAsync` | CM core summary, framework-level assessment |
| **Obligation Readiness** | `IObligationQueryService.GetSummaryAsync` | Stage unlock, obligation bands |
| **Stage Unlock Readiness** | `StageUnlockingService` band-filtered obligations | CM maturity stages, journey stepper |
| **Historical Readiness Trend** | `ComplianceSnapshots.AuditConfidenceScore` | Available in `JourneyTrendBuilder.ReadinessTrend` (not exposed as 4th API field — point-in-time via `accreditationReadinessPercent`) |

**Decision:** Do not merge journey and framework scores into one number. `IAnalyticsAggregationService.GetReadinessAsync` returns both with source labels for future Analytics Hub.

**Deprecated for new code:** Ad-hoc readiness calculations in module-specific query services.

---

# Deliverable 6 — KPI Ownership Catalog

| KPI | Business Meaning | Owner Service | Owner Projection | Owner Query | Consumers |
|-----|------------------|---------------|------------------|-------------|-----------|
| Readiness % (Journey) | Accreditation preparedness | `ComplianceScoreService` | Live OE scores | `GetJourneyScoreAsync` | Journey, AnalyticsAggregation |
| Readiness % (Framework) | Assessment compliance | `ComplianceScoringService` | OE assessments | `CalculateFrameworkScoreAsync` | CM, AnalyticsAggregation |
| Open Deficiencies | Active remediation workload | `DeficiencyMetricsService` | Live deficiencies | `GetCountsAsync` | DCC, CM, AnalyticsAggregation |
| Critical Deficiencies | Accreditation blockers | `DeficiencyMetricsService` | Live deficiencies | `IsCriticalDeficiency` predicate | DCC, Stage Unlock |
| CAPA Effectiveness | Closed effective vs ineffective | `CapaQueryService` | Live CAPA fields | `GetMetricsAsync(org, hospital, framework)` | DCC, CM via aggregation |
| Escalation Rate | % open with SLA escalated | `EscalationService` | Live + history | `GetMetricsForScopeAsync` | DCC, CM via aggregation |
| SLA Health | On-track + warning vs breached | `EscalationService` | Live SLA status | `GetMetricsForScopeAsync` | DCC, CM via aggregation |
| Open Audit Findings | Pre-promotion discovery | `DeficiencyMetricsService` | Live findings | `CountOpenFindingsAsync(org)` | DCC |
| Department Performance | CAPA/SLA by department | `CapaQueryService`, `EscalationService` | Live grouping | Scoped metrics | DCC, future scorecards |
| OE Score Trend | Framework score over time | `JourneyTrendBuilder` | `ComplianceSnapshots.OverallScore` | Snapshot query | Journey dashboard |
| Chapter Improvement Trend | Stage/chapter progress | `JourneyTrendBuilder` | `ChapterBreakdownJson` avg | Snapshot JSON parse | Journey dashboard |
| Risk Evolution Trend | Critical risk count history | `JourneyTrendBuilder` | `ComplianceSnapshots.CriticalRiskCount` | Snapshot query (empty if all zero) | Journey dashboard |

---

# Deliverable 7 — DCC Metric Delegation

**Refactored:** `DeficiencyCommandCenterQueryService`

| Former (internal calc) | Now Delegates To |
|------------------------|------------------|
| Open deficiency filters (inconsistent) | `DeficiencyMetricDefinitions.IsOpen` |
| CAPA KPI block | `IDeficiencyMetricsService.GetCapaMetricsAsync` → `CapaQueryService` |
| SLA/Escalation KPI block | `IDeficiencyMetricsService.GetSlaMetricsAsync` → `EscalationService.GetMetricsForScopeAsync` |
| Audit findings (was global) | `IDeficiencyMetricsService.CountOpenFindingsAsync(organizationId)` |

**Refactored:** `ComplianceMaturityAggregationService`

| Former | Now |
|--------|-----|
| Direct CAPA/SLA queries | `IAnalyticsAggregationService.GetGovernanceMetricsAsync` |
| Open gap count included Cancelled | Excludes Cancelled via `DeficiencyMetricDefinitions.IsOpen` |

---

# Deliverable 8 — Journey Trend Fix

**Problem:** `OeScoreTrend`, `ChapterImprovementTrend`, and `RiskEvolutionTrend` all received identical `OverallScore` series.

**Fix:** `JourneyTrendBuilder` + `JourneyWorkspaceAppService` wiring.

| API Field | Source | Empty When |
|-----------|--------|------------|
| `oeScoreTrend` | `ComplianceSnapshots.OverallScore` | No snapshots |
| `chapterImprovementTrend` | Average `CompliancePercent` from `ChapterBreakdownJson` | No chapter JSON / parse failure |
| `riskEvolutionTrend` | `ComplianceSnapshots.CriticalRiskCount` | All snapshot points have zero critical risk |

**Principle:** Do not fake data — unsupported series return empty arrays.

**Note:** Historical readiness trend (`AuditConfidenceScore`) is computed internally but not exposed as a fourth API field; headline readiness remains `accreditationReadinessPercent` (live journey score).

---

# Deliverable 9 — Organization Scope Validation

| Module | KPI | Scope | Status |
|--------|-----|-------|--------|
| Findings | Open findings | `TenantId == organizationId` | ✅ Already correct |
| DCC | Audit findings | Was unscoped | ✅ Fixed — org-scoped |
| DCC | Deficiencies | `OrganizationId` + hospital + framework | ✅ |
| CM | Governance metrics | Via `AnalyticsScope` | ✅ |
| CapaQueryService | CAPA metrics | `organizationId` + optional hospital/framework | ✅ Framework filter added |
| EscalationService | SLA metrics | `GetMetricsForScopeAsync(org, hospital, framework)` | ✅ |

**Rule:** All operational KPIs MUST filter by organization. Hospital/framework filters are additive scope dimensions.

---

# Deliverable 10 — Analytics Aggregation Service Foundation

**Location:** `ComplianceService/Application/Analytics/`

```
IAnalyticsAggregationService
  ├── GetDeficiencyCountsAsync(scope)
  ├── GetCapaMetricsAsync(scope)
  ├── GetSlaMetricsAsync(scope)
  ├── GetReadinessAsync(scope)        → journey + framework scores with source labels
  └── GetGovernanceMetricsAsync(scope) → CAPA + SLA summaries for CM/DCC

IDeficiencyMetricsService
  ├── GetCountsAsync(scope)
  ├── GetScopedOpenAsync(scope)
  ├── CountOpenFindingsAsync(orgId)
  ├── GetCapaMetricsAsync(scope)
  └── GetSlaMetricsAsync(scope)

AnalyticsScope { OrganizationId, HospitalId, FrameworkId }
```

**DI Registration (Program.cs):**

- `IDeficiencyMetricsService` → `DeficiencyMetricsService`
- `IAnalyticsAggregationService` → `AnalyticsAggregationService`
- `IDeficiencyComplianceEventService` → `DeficiencyComplianceEventService`

**No HTTP endpoints added** — service layer only for 12B-1 consumers.

---

# Deliverable 11 — Validation Report

## Pipeline Validated (Code Path)

```
Finding Created
  → FindingCreated ComplianceEvent
  → (optional) Promotion
  → DeficiencyCreated + DeficiencyIdentifiedDomainEvent → outbox → snapshot queue
  → TaskCreated
  → EvidenceUploaded / EvidenceApproved → snapshot queue
  → DeficiencyVerified/Closed → status ComplianceEvent + domain event → snapshot queue
  → CAPA updates → Corrective/Preventive/Effectiveness ComplianceEvents
  → SLA evaluation → DeficiencySla* ComplianceEvents
  → Escalation → DeficiencyEscalated / EscalationAcknowledged / EscalationResolved
  → Readiness → GetJourneyScoreAsync + snapshot recalc → ComplianceSnapshots
```

## Automated Validation

| Check | Result |
|-------|--------|
| `dotnet build ComplianceService` | ✅ Pass |
| `dotnet test ComplianceService.Tests` | ✅ 188/188 pass |
| CM integration tests with new aggregation stub | ✅ Pass |

## Manual Validation Recommended (12B-1)

- End-to-end promotion → deficiency → close with snapshot delta verification
- Verify three journey trend arrays differ in staging with real snapshot history
- Confirm DCC findings count matches Findings module for same org

---

# Deliverable 12 — Sprint 12B-1 Readiness Assessment

## Ready for 12B-1

| Capability | Status |
|------------|--------|
| KPI SSOT definitions | ✅ |
| Metrics service layer | ✅ |
| Event types standardized | ✅ |
| Outbox → snapshot path for deficiencies | ✅ |
| DCC/CM metric delegation | ✅ |
| Journey trend integrity | ✅ |
| Org-scoped operational KPIs | ✅ |

## Deferred to 12B-1+

| Item | Reason |
|------|--------|
| Analytics Hub UI / charts | Out of 12B-0 scope |
| `GET /analytics/*` API endpoints | 12B-1 platform foundation |
| Historical CAPA/SLA/deficiency fact tables | Trend analytics need daily rollups |
| Unified `SlaStatusChanged` emission in `SlaEvaluationService` | Legacy SLA events still work; consolidation optional |
| Fourth journey trend field for readiness history | API contract unchanged; can add in 12B-2 |
| Remove unused `_escalationService`/`_capaQuery` from CM ctor | Low priority cleanup |
| Analytics fact tables / dimensional model | 12B-1 architecture |

## Known Risks

| Risk | Mitigation |
|------|------------|
| Journey vs framework readiness divergence | Documented in `ReadinessMetricDefinitions`; aggregation service exposes both |
| CM CriticalCount vs DCC critical deficiencies | Documented in `CriticalSeverityDefinitions` |
| CAPA/SLA events don't trigger snapshot recalc | Acceptable — live metrics; historical trends need 12B-1 fact tables |
| Chapter trend sparse if snapshot JSON missing | Empty array returned — UI should handle gracefully |

---

# Success Criteria Checklist

| Criterion | Status |
|-----------|--------|
| Every KPI has one definition | ✅ `Application/Analytics/*Definitions.cs` |
| Every KPI has one owner | ✅ KPI Ownership Catalog |
| Projection refreshes reliable for deficiency lifecycle | ✅ Outbox + processor fixed |
| Event gaps eliminated | ✅ Critical gaps closed; SLA unified event optional |
| DCC and CM consume consistent metrics | ✅ Delegated to metrics/aggregation services |
| Journey trends accurate | ✅ Distinct series from snapshots |
| Organization scoping consistent | ✅ DCC findings fixed |
| Analytics aggregation layer exists | ✅ `IAnalyticsAggregationService` |
| Analytics Hub can be built without duplication | ✅ Ready |
| Ready for Sprint 12B-1 | ✅ |

---

# File Index (New / Modified)

## New Files

```
ComplianceService/Application/Analytics/
  AnalyticsAggregationService.cs
  AnalyticsScope.cs
  CriticalSeverityDefinitions.cs
  DeficiencyComplianceEventService.cs
  DeficiencyMetricCountsDto.cs
  DeficiencyMetricDefinitions.cs
  DeficiencyMetricsService.cs
  IAnalyticsServices.cs
  JourneyTrendBuilder.cs
  ReadinessMetricDefinitions.cs
```

## Key Modified Files

```
ComplianceService/Program.cs                                    — DI registration
ComplianceService/Application/Events/ComplianceEventType.cs       — events 31–39
ComplianceService/Application/Services/OutboxDomainEventDispatcher.cs
ComplianceService/Application/Services/InProcessOutboxProcessor.cs
ComplianceService/Domain/ComplianceDeficiency.cs                — Cancel() event
ComplianceService/Application/Services/DeficiencyCommandCenterQueryService.cs
ComplianceService/Application/Services/ComplianceMaturityAggregationService.cs
ComplianceService/Application/Services/JourneyWorkspaceAppService.cs
ComplianceService/Application/Services/EscalationService.cs
ComplianceService/Application/Services/CapaQueryService.cs
ComplianceService/Application/Services/PatchDeficiency*CommandHandler*.cs
ComplianceService.Tests/ComplianceMaturityDashboardIntegrationTests.cs
```

---

**End of Sprint 12B-0 — Analytics Stabilization Report**
