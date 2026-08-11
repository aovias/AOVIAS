# Sprint 13B – Compliance Copilot Recommendation Engine

**Date:** 2026-06-24  
**Sprint:** 13B  
**Status:** ✅ Complete  
**Sequence:** Sprint 13A → **Sprint 13B** → Sprint 13C (Draft Generation) → Sprint 13D (Approval Cards)

---

## Objective

Generate **explainable, traceable, SSOT-backed recommendations** for hospital users.

Recommendations originate exclusively from operational truth — not LLM imagination.

### Architecture Principle

```
Operational Data (SSOT services)
          ↓
 Rule Engine (deterministic)
          ↓
 CopilotRecommendation (entity)
          ↓
 Template-based Explainer (Sprint 13B)
 (replaceable with LLM in Sprint 13C+)
          ↓
          User
```

---

## Critical Rules Enforced

| Rule | Status |
|------|--------|
| No autonomous actions | ✅ |
| No task creation | ✅ |
| No CAPA creation | ✅ |
| No finding creation | ✅ |
| No compliance status modification | ✅ |
| No write tool invocations | ✅ |
| Recommendations only | ✅ |

---

## Phase 1 — Recommendation Taxonomy

### RecommendationCategory (12 values)

| Value | Domain |
|-------|--------|
| Accreditation | Stage unlock, maturity progression |
| Deficiency | Compliance deficiencies |
| CAPA | Corrective actions |
| SLA | Service level agreements |
| Escalation | Active escalations |
| Evidence | Evidence upload and management |
| Verification | Auditor verification backlog |
| Task | Compliance tasks |
| Executive | Board-level risk |
| Department | Department-level performance |
| Risk | Risk exposure |
| Analytics | Analytics-derived insight |

**File:** `Domain/Copilot/Enums/RecommendationCategory.cs`

### RecommendationSeverity

| Value | Ranking Score | Use |
|-------|---------------|-----|
| Critical | 100 | Blocking stage unlock, board-level risk |
| High | 75 | Urgent action required |
| Medium | 50 | Action recommended |
| Low | 25 | Improvement opportunity |

**File:** `Domain/Copilot/Enums/RecommendationSeverity.cs`

---

## Phase 2 — Enhanced Recommendation Entity

**New fields added to `CopilotRecommendation`:**

| Field | Type | Purpose |
|-------|------|---------|
| `Category` | `RecommendationCategory` | Domain classification |
| `Severity` | `RecommendationSeverity` | Urgency level |
| `Confidence` | `decimal(5,4)` | Rule confidence (0–1) |
| `SourceEntityType` | `string?` | Source entity type (e.g. "ComplianceDeficiency") |
| `SourceEntityId` | `Guid?` | Source entity ID for deep inspection |
| `DeepLink` | `string?` | Frontend deep-link URL for one-click navigation |
| `ExpiresAtUtc` | `DateTime?` | Auto-expiry time |
| `RationaleJson` | `string?` (nvarchar max) | Machine-readable `RecommendationRationale` |

**New factory:** `CopilotRecommendation.CreateFromRuleEngine(...)` — used by the rule engine.

**Migration:** `20260624130000_Sprint13B_RecommendationEngine.cs`

### RecommendationRationale Schema (JSON)

Every recommendation stores this in `RationaleJson`:

```json
{
  "ruleName": "deficiency.critical_open",
  "metrics": {
    "CriticalOpen": "3",
    "TotalOpen": "12",
    "BlockingOpen": "2"
  },
  "thresholds": {
    "MaxAllowed": "0"
  },
  "sourceEntities": [
    { "entityType": "ComplianceDeficiency", "entityId": "", "entityRef": "" }
  ],
  "dataAsOfUtc": "2026-06-24T13:00:00Z",
  "scopeRef": "org-id/hosp-id/fw-id"
}
```

---

## Phase 3 — Rules Framework

### Architecture

```
IRecommendationRule              — contract for all rules
RecommendationRuleBase           — abstract base with helpers
RecommendationRuleContext        — shared pre-fetched data
RecommendationCandidate          — intermediate value object
IRecommendationEngine            — orchestrator
RecommendationEngine             — implementation
IRecommendationRankingService    — multi-factor scorer
IRecommendationExplainerService  — explanation layer
```

### Rule Evaluation Pipeline

1. `RecommendationEngine.GenerateAsync(orgId, hospitalId, frameworkId, request)`
2. Resolve `AnalyticsScope`
3. **Parallel data fetch** from 7 SSOT services
4. **Parallel rule execution** across all registered `IRecommendationRule` implementations
5. `IRecommendationExplainerService.Explain()` — populates Description for each candidate
6. `IRecommendationRankingService.Rank()` — scores all candidates
7. `FilterTopPerPersona(maxPerPersona: 5)` — top-N per persona
8. `ExpireStaleAsync()` — expires recommendations older than 72 hours
9. `BulkCreateAsync()` — persists batch

---

## Phase 4–9 — Rule Catalog

### Accreditation Rules

| Rule | Trigger | Source Service | Personas |
|------|---------|----------------|----------|
| `accreditation.stage_unlock_blocked` | Stage not criteria-met | `IStageUnlockingService` | AccreditationCoach, QualityManager |
| `accreditation.missing_evidence` | Evidence.Missing > 0 | `IEvidenceLibraryService` | AccreditationCoach, EvidenceAdvisor, QualityManager |
| `accreditation.verification_backlog` | PendingReview ≥ 3 | `IEvidenceLibraryService` | AccreditationCoach, AuditorAssistant, EvidenceAdvisor |
| `accreditation.readiness_gap` | Readiness < 75% | `IAnalyticsAggregationService` | AccreditationCoach, QualityManager, Executive, DeptHead |

### Deficiency Rules

| Rule | Trigger | Source Service | Personas |
|------|---------|----------------|----------|
| `deficiency.critical_open` | CriticalOpen > 0 | `IDeficiencyMetricsService` | QualityManager, CapaAdvisor, AccreditationCoach, Executive |
| `deficiency.recurring_pattern` | RecurringOpen > 0 | `IDeficiencyMetricsService` | QualityManager, CapaAdvisor |
| `deficiency.aging_open` | EscalatedOpen+BlockingOpen > 0 | `IDeficiencyMetricsService` | QualityManager, CapaAdvisor, DeptHead |
| `deficiency.audit_origin_unactioned` | AuditOriginOpen > 0 | `IDeficiencyMetricsService` | QualityManager, AuditorAssistant, CapaAdvisor |

### CAPA Rules

| Rule | Trigger | Source Service | Personas |
|------|---------|----------------|----------|
| `capa.overdue_actions` | OverdueCapas > 0 | `IDeficiencyMetricsService` (CAPA) | CapaAdvisor, QualityManager |
| `capa.ineffective_actions` | IneffectiveRate ≥ 20% (min sample 3) | `IDeficiencyMetricsService` (CAPA) | CapaAdvisor, QualityManager, Executive |
| `capa.recurring_pattern` | RecurrenceRate ≥ 15% (min 2 recurrences) | `IDeficiencyMetricsService` (CAPA) | CapaAdvisor, QualityManager |

### SLA Rules

| Rule | Trigger | Source Service | Personas |
|------|---------|----------------|----------|
| `sla.breach_risk` | AtRiskCount + BreachedCount > 0 | `IEscalationService` | QualityManager, CapaAdvisor, DeptHead |
| `sla.active_escalations` | EscalatedCount > 0 | `IEscalationService` | Executive, QualityManager, DeptHead |

### Evidence Rules

| Rule | Trigger | Source Service | Personas |
|------|---------|----------------|----------|
| `evidence.missing_obligation_coverage` | Missing ≥ 1 | `IEvidenceLibraryService` | EvidenceAdvisor, AuditorAssistant |
| `evidence.pending_verification_queue` | PendingReview ≥ 5 | `IEvidenceLibraryService` | AuditorAssistant, EvidenceAdvisor |

### Executive Rules

| Rule | Trigger | Source Service | Personas |
|------|---------|----------------|----------|
| `executive.readiness_concern` | JourneyReadiness < 65% | `IAnalyticsAggregationService` | Executive, DeptHead |
| `executive.critical_escalation_exposure` | CriticalEscalations > 0 | `IEscalationService` | Executive |
| `executive.department_sla_risk` | Dept SLA compliance < 60% | `IEscalationService` | Executive, DeptHead |

---

## Phase 10 — Ranking Engine

**File:** `Application/Copilot/Services/RecommendationRankingService.cs`

### Scoring Formula

```
RankScore = (SeverityScore × 0.40)
           + (CategoryUrgency × 0.25)
           + (Confidence × 0.20)
           + (RecurrenceBoost × 0.15)
```

### Category Urgency Weights

| Category | Urgency Score |
|----------|--------------|
| Accreditation | 100 |
| Risk | 95 |
| Escalation | 90 |
| SLA | 85 |
| Deficiency | 80 |
| CAPA | 75 |
| Evidence | 70 |
| Verification | 65 |
| Executive | 60 |
| Department | 55 |

### Output

- Top 5 per persona (configurable via `maxPerPersona`)
- Priority 1 = highest urgency
- Recommendations expire after 72 hours (auto-expired on next refresh)

---

## Phase 11 — Persona-Specific Filtering

| Persona | Primary Categories |
|---------|--------------------|
| AccreditationCoach | Accreditation, Evidence, Verification |
| QualityManagerAssistant | Deficiency, CAPA, SLA, Accreditation |
| EvidenceAdvisor | Evidence, Verification |
| CapaAdvisor | CAPA, Deficiency, SLA |
| ExecutiveAdvisor | Executive, Risk, Escalation |
| DepartmentHeadAssistant | Deficiency, SLA, Accreditation, Department |
| AuditorAssistant | Evidence, Verification, Deficiency |

---

## Phase 12 — LLM Explanation Layer

**Status:** Template-based implementation (Sprint 13B).  
**Interface:** `IRecommendationExplainerService`  
**Implementation:** `RecommendationExplainerService` (deterministic templates)

### Architecture

```
RecommendationCandidate.RationaleData
         ↓
IRecommendationExplainerService.Explain()
         ↓
CopilotRecommendation.Description (populated)
```

The recommendation **content** (title, rationale, metrics) is always rule-based.  
The `Description` is the **presentation layer** — generated from templates in Sprint 13B.

### Sprint 13C upgrade path

Replace `RecommendationExplainerService` with an `LlmRecommendationExplainerService` that:
1. Receives `RecommendationCandidate` (structured data)
2. Calls AIOrchestratorService with the rationale + metrics
3. Returns an LLM-generated executive-friendly explanation
4. Stores in `Description`

The recommendation content remains deterministic — LLM only improves readability.

---

## Phase 13 — API Surface

### ComplianceService Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/copilot/recommendations` | All recommendations for org |
| GET | `/api/v1/copilot/recommendations/persona/{persona}` | Top-5 for persona |
| GET | `/api/v1/copilot/recommendations/{id}` | Single recommendation |
| POST | `/api/v1/copilot/recommendations/refresh` | Trigger rule engine |

### Gateway Endpoints (proxied)

Same paths under `/api/v1/copilot/*` — Gateway proxies via MediatR handlers.

### Refresh Request Body

```json
{
  "frameworkId": "optional-guid",
  "persona": "optional-string",
  "context": { "pageContext": "...", "focusedEntityId": "..." }
}
```

---

## Phase 14 — Frontend Integration

### CopilotStore (Extended)

**New signals:**

| Signal | Type | Description |
|--------|------|-------------|
| `topRecommendations` | `computed` | Top 5 across all personas |
| `personaRecommendations` | `computed` | Top 5 for active persona |
| `urgentRecommendations` | `computed` | Critical + High severity |
| `recommendationCounts` | `computed` | Count by severity |

**New actions:**

| Method | Description |
|--------|-------------|
| `refreshRecommendations(frameworkId?)` | Triggers POST /refresh |
| `loadPersonaRecommendations(persona?)` | Loads persona-specific recommendations |

### New Components

| Component | Selector | Description |
|-----------|----------|-------------|
| `RecommendationListComponent` | `app-recommendation-list` | Reactive list with refresh |
| `RecommendationCardComponent` | `app-recommendation-card` | Single recommendation display |

---

## Database Schema Changes

### CopilotRecommendations (new columns — Sprint 13B)

| Column | Type | Default |
|--------|------|---------|
| `Category` | int | 0 (Accreditation) |
| `Severity` | int | 1 (Medium) |
| `Confidence` | decimal(5,4) | 1.0 |
| `SourceEntityType` | nvarchar(100) | NULL |
| `SourceEntityId` | uniqueidentifier | NULL |
| `DeepLink` | nvarchar(500) | NULL |
| `RationaleJson` | nvarchar(max) | NULL |
| `ExpiresAtUtc` | datetime2 | NULL |

### New Indexes

- `IX_CopilotRecommendations_Category`
- `IX_CopilotRecommendations_Severity`
- `IX_CopilotRecommendations_OrgPersonaStatus` (composite)

---

## Validation Scenarios

| Scenario | Expected Recommendation | Rule |
|----------|------------------------|------|
| Stage Commitment blocked by 3 critical deficiencies | "Commitment stage progression blocked" | `accreditation.stage_unlock_blocked` |
| 7 missing evidence items | "Upload 7 missing evidence items" | `accreditation.missing_evidence` |
| 4 overdue CAPA actions | "Complete 4 overdue CAPA actions immediately" | `capa.overdue_actions` |
| 2 SLA breaches | "2 SLA breaches + N at-risk" | `sla.breach_risk` |
| 3 recurring deficiencies | "Review 3 recurring deficiency patterns" | `deficiency.recurring_pattern` |
| Executive: readiness at 52% | "Executive attention required — readiness at 52%" | `executive.readiness_concern` |
| 2 departments below 60% SLA compliance | "2 departments at compliance risk" | `executive.department_sla_risk` |

### Traceability Guarantees

- Every recommendation has `RationaleJson` (machine-readable evidence trail)
- Every recommendation has `RuleName` in rationale (auditable source)
- Every recommendation has `DataAsOfUtc` (temporal traceability)
- No recommendation is generated without corresponding SSOT data trigger
- No hallucinations — LLM is not involved in recommendation generation

---

## Dependency Map

| Rule | SSOT Service | Data Field |
|------|-------------|------------|
| StageUnlockBlockedRule | `IStageUnlockingService` | `BlockingReasons`, `ProgressPercent` |
| MissingEvidenceRule | `IEvidenceLibraryService` | `EvidenceSummaryDto.Missing` |
| VerificationBacklogRule | `IEvidenceLibraryService` | `EvidenceSummaryDto.PendingReview` |
| ReadinessGapRule | `IAnalyticsAggregationService` | `AnalyticsReadinessDto.JourneyAccreditationReadiness` |
| CriticalDeficiencyRule | `IDeficiencyMetricsService` | `DeficiencyMetricCountsDto.CriticalOpen` |
| RecurringDeficiencyRule | `IDeficiencyMetricsService` | `DeficiencyMetricCountsDto.RecurringOpen` |
| AgingDeficiencyRule | `IDeficiencyMetricsService` | `DeficiencyMetricCountsDto.EscalatedOpen` |
| UnassignedDeficiencyRule | `IDeficiencyMetricsService` | `DeficiencyMetricCountsDto.AuditOriginOpen` |
| OverdueCAPARule | `IDeficiencyMetricsService` | `CapaMetricsDto.OverdueCapas` |
| IneffectiveCAPARule | `IDeficiencyMetricsService` | `CapaMetricsDto.IneffectiveCapas`, `CapaEffectivenessRate` |
| RecurringPatternRule | `IDeficiencyMetricsService` | `CapaMetricsDto.RecurrenceRate` |
| SlaBreachRiskRule | `IEscalationService` | `SlaEscalationMetricsDto.AtRiskCount`, `BreachedCount` |
| EscalationCandidateRule | `IEscalationService` | `SlaEscalationMetricsDto.CriticalEscalations` |
| MissingEvidenceObligationRule | `IEvidenceLibraryService` | `EvidenceSummaryDto.Missing` |
| PendingVerificationRule | `IEvidenceLibraryService` | `EvidenceSummaryDto.PendingReview` |
| ReadinessDeclineRule | `IAnalyticsAggregationService` | `AnalyticsReadinessDto.JourneyAccreditationReadiness` |
| CriticalEscalationExposureRule | `IEscalationService` | `SlaEscalationMetricsDto.CriticalEscalations` |
| DepartmentRiskRule | `IEscalationService` | `SlaEscalationMetricsDto.DepartmentSlaCompliance` |

---

## Sprint 13C Readiness Assessment

Sprint 13B is **fully ready** for Sprint 13C — Draft Generation Engine.

### What Sprint 13C will build on

| Capability | Sprint 13B Status |
|------------|------------------|
| `CopilotRecommendation` entity with full traceability | ✅ Ready |
| `RecommendationRationale` (structured data for LLM) | ✅ Ready |
| `IRecommendationExplainerService` interface | ✅ Replaceable with LLM |
| `AiDraft` entity (Sprint 13A) | ✅ Ready for content |
| `ICopilotDraftService` contract (Sprint 13A) | ✅ Ready for implementation |
| `IAiDraftRepository` (Sprint 13A) | ✅ Ready |
| Deep links for draft context | ✅ Ready |
| Persona routing | ✅ Ready |

### Sprint 13C Entry Points

1. **Implement `ICopilotDraftService.GenerateDraftAsync()`** — triggers LLM draft generation using `CopilotRecommendation.RationaleJson` as context input
2. **Replace/augment `RecommendationExplainerService`** — with LLM-based explainer using AIOrchestratorService
3. **Add `POST /copilot/drafts/generate`** endpoint
4. **Wire `AiDraft` to specific `CopilotRecommendation`** via a foreign key linkage
5. **Extend frontend store** with `generateDraft(recommendationId)` action

### Data available to Sprint 13C LLM

Every recommendation in the database contains:
- Structured `RationaleJson` (metrics, thresholds, source entities)
- `RecommendationType` (rule name)
- `Category` + `Severity` (for tone/urgency)
- `Persona` (for audience-appropriate language)
- `DeepLink` (for contextual navigation in drafts)
- `SourceEntityType` + `SourceEntityId` (for entity-specific context loading)

---

## Files Created / Modified

### New Files (Sprint 13B)

| Path | Description |
|------|-------------|
| `Domain/Copilot/Enums/RecommendationCategory.cs` | 12-value category enum |
| `Domain/Copilot/Enums/RecommendationSeverity.cs` | 4-value severity enum |
| `Domain/Copilot/RecommendationRationale.cs` | Machine-readable rationale VO |
| `Application/Copilot/Models/RecommendationCandidate.cs` | Rule output VO |
| `Application/Copilot/Models/RecommendationRuleContext.cs` | Shared rule context |
| `Application/Copilot/Models/RefreshRecommendationsRequest.cs` | Refresh DTO |
| `Application/Copilot/Contracts/IRecommendationRule.cs` | Rule interface |
| `Application/Copilot/Contracts/IRecommendationEngine.cs` | Engine interface |
| `Application/Copilot/Contracts/IRecommendationRankingService.cs` | Ranker interface |
| `Application/Copilot/Contracts/IRecommendationExplainerService.cs` | Explainer interface |
| `Application/Copilot/Rules/RecommendationRuleBase.cs` | Abstract base |
| `Application/Copilot/Rules/Accreditation/StageUnlockBlockedRule.cs` | Accreditation rule |
| `Application/Copilot/Rules/Accreditation/MissingEvidenceRule.cs` | Evidence rule |
| `Application/Copilot/Rules/Accreditation/VerificationBacklogRule.cs` | Verification rule |
| `Application/Copilot/Rules/Accreditation/ReadinessGapRule.cs` | Readiness rule |
| `Application/Copilot/Rules/Deficiency/CriticalDeficiencyRule.cs` | Critical deficiency |
| `Application/Copilot/Rules/Deficiency/RecurringDeficiencyRule.cs` | Recurring pattern |
| `Application/Copilot/Rules/Deficiency/AgingDeficiencyRule.cs` | Aging/escalated |
| `Application/Copilot/Rules/Deficiency/UnassignedDeficiencyRule.cs` | Audit-origin |
| `Application/Copilot/Rules/Capa/OverdueCAPARule.cs` | Overdue CAPA |
| `Application/Copilot/Rules/Capa/IneffectiveCAPARule.cs` | Ineffective CAPA |
| `Application/Copilot/Rules/Capa/RecurringPatternRule.cs` | CAPA recurrence |
| `Application/Copilot/Rules/Sla/SlaBreachRiskRule.cs` | SLA breach risk |
| `Application/Copilot/Rules/Sla/EscalationCandidateRule.cs` | Active escalations |
| `Application/Copilot/Rules/Evidence/MissingEvidenceObligationRule.cs` | Missing evidence |
| `Application/Copilot/Rules/Evidence/PendingVerificationRule.cs` | Pending verification |
| `Application/Copilot/Rules/Executive/ReadinessDeclineRule.cs` | Executive readiness |
| `Application/Copilot/Rules/Executive/CriticalEscalationExposureRule.cs` | Critical escalations |
| `Application/Copilot/Rules/Executive/DepartmentRiskRule.cs` | Department SLA risk |
| `Application/Copilot/Services/RecommendationEngine.cs` | Engine implementation |
| `Application/Copilot/Services/RecommendationRankingService.cs` | Ranking service |
| `Application/Copilot/Services/RecommendationExplainerService.cs` | Template explainer |
| `Application/Copilot/Services/DeepLinkBuilder.cs` | Deep link constants |
| `Migrations/20260624130000_Sprint13B_RecommendationEngine.cs` | DB migration |
| `WebApp/features/copilot/components/recommendation-card/*` | Card component |
| `WebApp/features/copilot/components/recommendation-list/*` | List component |

### Modified Files (Sprint 13B)

| Path | Change |
|------|--------|
| `Domain/Copilot/CopilotRecommendation.cs` | New fields + `CreateFromRuleEngine` factory |
| `Infrastructure/Copilot/EntityConfigurations/CopilotRecommendationConfiguration.cs` | New column configs + indexes |
| `Infrastructure/Copilot/Repositories/CopilotRecommendationRepository.cs` | `BulkCreateAsync`, `ExpireStaleAsync`, `GetByPersonaAsync` |
| `Application/Copilot/Contracts/ICopilotRecommendationRepository.cs` | New method signatures |
| `Application/Copilot/Contracts/ICopilotRecommendationService.cs` | `RefreshAsync`, `GetByPersonaAsync` |
| `Application/Copilot/Services/CopilotRecommendationService.cs` | Engine integration, full DTO mapping |
| `Application/Copilot/DTOs/CopilotRecommendationDto.cs` | New fields |
| `Controllers/V1/Copilot/CopilotRecommendationsController.cs` | `/persona/{persona}` + `/refresh` endpoints |
| `Gateway/Application/Queries/CopilotQueries.cs` | New query records |
| `Gateway/Application/Handlers/CopilotQueryHandlers.cs` | New handlers |
| `Gateway/Controllers/CopilotController.cs` | New routes |
| `Program.cs` | 19 new DI registrations |
| `WebApp/features/copilot/models/copilot-recommendation.model.ts` | New enums + fields |
| `WebApp/features/copilot/services/copilot-api.service.ts` | Persona + refresh API methods |
| `WebApp/features/copilot/state/copilot.store.ts` | New computed signals + refresh action |

---

*Sprint 13B complete. The PRAMAN Copilot now delivers deterministic, SSOT-backed, persona-aware recommendations with full evidence traceability. Ready for Sprint 13C — Draft Generation Engine.*
