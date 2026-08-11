# Sprint 13C — Compliance Copilot Draft Generation Engine

**Date:** 2026-06-24  
**Sprint:** 13C  
**Sprint Name:** Draft Generation Engine  
**Objective:** Transform rule-engine recommendations into human-editable, auditable, versioned, organization-scoped drafts.

---

## Summary

Sprint 13C completes the recommendation → draft pipeline. Users can now generate structured, auditable compliance drafts from any Copilot recommendation card. Every draft is:

- **Human-editable** — content is markdown, modifiable by human reviewers
- **Auditable** — Source Intelligence metadata embedded in every draft
- **Versioned** — regeneration creates a new version; originals are never overwritten
- **Organization-scoped** — all drafts are isolated to their organization
- **Advisory only** — no workflow mutations, task creation, or compliance state changes occur

---

## Critical Design Principles (Enforced)

| Principle | Status |
|-----------|--------|
| No task creation | ✅ Enforced |
| No CAPA creation | ✅ Enforced |
| No finding creation | ✅ Enforced |
| No deficiency modification | ✅ Enforced |
| No evidence approval | ✅ Enforced |
| No workflow execution | ✅ Enforced |
| Human review required | ✅ Advisory notices in every draft |

---

## Architecture Overview

```
RecommendationCard (Frontend)
    ↓ "Generate CAPA Draft" click
CopilotStore.generateDraftFromRecommendation()
    ↓ POST /api/v1/copilot/drafts/generate
Gateway → ComplianceService
    ↓
CopilotDraftsController.Generate()
    ↓
ICopilotDraftService.GenerateDraftAsync()
    ↓
IDraftGenerationService.GenerateAsync()
    ├── DraftTemplateRegistry.Resolve(DraftType)
    ├── BuildContextAsync() → batch-fetch analytics, CAPA, SLA, readiness
    ├── IDraftContentGenerator.GenerateAsync(template, context)
    │   └── IDraftTemplate.BuildSections(context)
    │   └── IDraftTemplate.BuildSourceIntelligence(context)
    ├── AiDraft.CreateFromEngine(...)
    └── IAiDraftRepository.CreateAsync(draft)
    ↓
AiDraftDto → Frontend
    ↓
DraftViewerComponent (read-only preview)
```

---

## Template Catalog

| Template | DraftType | Primary Source | Sections |
|----------|-----------|----------------|----------|
| `CapaDraftTemplate` | `CapaDraft` (0) | Deficiency, CAPA | Problem Statement, Observed Deficiency, Root Cause Summary, Corrective Action, Preventive Action, Success Criteria, Target Completion, Effectiveness Review Plan |
| `ExecutiveComplianceBriefingTemplate` | `ExecutiveComplianceBriefing` (1) | Analytics | Executive Summary, What Changed, Current Risks, Readiness Status, Critical Deficiencies, CAPA Health, SLA Health, Escalations, Recommendations, Leadership Attention Items |
| `ReadinessSummaryTemplate` | `ReadinessSummary` (2) | Maturity, Stage Unlock | Current Stage, Readiness Score, Unlock Progress, Blocking Issues, Evidence Gaps, Verification Gaps, Department Performance, Next Milestones, Recommended Actions |
| `EvidenceChecklistTemplate` | `EvidenceChecklist` (3) | Obligations, Evidence | Objective Element, Required Evidence, Missing Evidence, Verification Status, Upload Recommendations |

---

## Recommendation → Draft Mapping

| Recommendation Category | Generated Draft Type |
|-------------------------|---------------------|
| Deficiency | `CapaDraft` |
| CAPA | `CapaDraft` |
| Risk | `CapaDraft` |
| Accreditation | `ReadinessSummary` |
| Verification | `EvidenceChecklist` |
| Evidence | `EvidenceChecklist` |
| Executive | `ExecutiveComplianceBriefing` |
| Analytics | `ExecutiveComplianceBriefing` |
| Escalation | `ExecutiveComplianceBriefing` |
| Department | `ReadinessSummary` |

---

## Source Intelligence Architecture

Every generated draft contains a `<!-- SOURCE_INTELLIGENCE_BEGIN ... SOURCE_INTELLIGENCE_END -->` comment block in its markdown content and a serialized `DraftSourceIntelligence` JSON in `MetadataJson`.

### `DraftSourceIntelligence` Schema

```json
{
  "recommendationIds": ["<guid>"],
  "kpiValues": {
    "TotalDeficiencies": "24",
    "CriticalOpenDeficiencies": "5",
    "JourneyReadinessScore": "74.2"
  },
  "deficiencyRefs": ["<entity-id>"],
  "capaRefs": ["Root Cause A", "Root Cause B"],
  "obligationRefs": ["Obligation Name 1"],
  "analyticsMetrics": {
    "DeficiencyOpen": "12",
    "CapaEffectivenessRate": "78%",
    "SlaBreachRate": "15%"
  },
  "rationaleObjects": ["{...rationale json...}"],
  "templateName": "CAPA Proposal Draft",
  "generatorName": "TemplateDraftContentGenerator",
  "organizationId": "<org-id>",
  "generatedAtUtc": "2026-06-24T12:00:00Z",
  "schemaVersion": "1.0"
}
```

### Purpose
- **Auditability**: Reviewers can trace exactly what data drove the draft content
- **Explainability**: Support teams can explain why specific sections were generated
- **Regeneration reliability**: Re-generation uses same data sources for consistency
- **Future approvals (Sprint 13D)**: Approval card framework can validate data currency

---

## Versioning Strategy

### Design Principle: Immutable Versions
- Each regeneration creates a **new `AiDraft` entity** — originals are never overwritten
- All versions share a `GroupId` (Guid) which links the version chain
- `GetVersionsByGroupAsync(groupId)` returns the full audit trail
- Version numbers start at 1 and increment sequentially

### Version Table Structure
```
GroupId          | Id    | Version | Status   | GeneratedAt
------------------------------------------------------------------
7f3a...          | aaa1  | 1       | Reviewed | 2026-06-24 10:00
7f3a...          | bbb2  | 2       | Draft    | 2026-06-24 14:30
7f3a...          | ccc3  | 3       | Draft    | 2026-06-24 18:00  ← latest
```

---

## Content Generation Strategy

### Sprint 13C: Deterministic Template Generator
`TemplateDraftContentGenerator` — produces structured markdown from pre-fetched operational data. No LLM dependency. 100% deterministic and testable.

### Sprint 13D+: LLM Extension Point
`LlmDraftContentGenerator` — registered but throws `NotImplementedException`. To activate:
1. Inject `IAiProvider` into `LlmDraftContentGenerator`
2. Replace DI registration in `Program.cs`
3. Pass template structure as system prompt + operational context as JSON
4. LLM enriches section content while Source Intelligence remains deterministic

---

## Recommendation Impact Model

`RecommendationImpact` record is now persisted with every recommendation via `ImpactJson`:

```csharp
public sealed record RecommendationImpact
{
    decimal EstimatedReadinessImpact;     // points gained on 0-100 scale
    decimal EstimatedCmiImpact;           // CMI improvement
    int EstimatedDeficienciesReduced;     // count
    int EstimatedSlaBreachesPrevented;    // count
    int EstimatedCapaImprovements;        // count
}
```

### Estimation Logic
| Category | Readiness Impact | Method |
|----------|-----------------|--------|
| Deficiency (Critical) | 5.0 pts | Per critical deficiency |
| CAPA | 2.5 pts | Per overdue CAPA resolved |
| Accreditation | Gap × 0.6 | Readiness gap fill |
| Evidence | 3.0 pts | Evidence coverage |
| SLA/Escalation | 1.5 pts | Breach prevention |
| Executive | 4.0 pts | Governance improvement |

---

## Database Changes

### Migration: `Sprint13C_DraftGeneration`

**AiDrafts table:**

| Column | Type | Notes |
|--------|------|-------|
| `GroupId` | `uniqueidentifier NOT NULL` | Version group key; back-filled to `Id` for existing rows |
| `DraftType` | `int NOT NULL` | Changed from `nvarchar(100)` to typed int enum |
| `SourceType` | `int NOT NULL` | New — source type enum |
| `SourceEntityId` | `uniqueidentifier NULL` | New — source entity reference |
| `RecommendationId` | `uniqueidentifier NULL` | New — source recommendation |
| `Summary` | `nvarchar(2000)` | New — executive summary |
| `GeneratedBy` | `nvarchar(200)` | New — generator name |
| `Confidence` | `decimal(5,4)` | New — generation confidence |
| `MetadataJson` | `nvarchar(max)` | Replaces `SourceContext` — full Source Intelligence |
| `GeneratedAtUtc` | `datetime2` | New — generation timestamp |

**New Indexes:** `IX_AiDrafts_GroupId`, `IX_AiDrafts_DraftType`, `IX_AiDrafts_RecommendationId`, `IX_AiDrafts_OrgId_GroupId`

**CopilotRecommendations table:**

| Column | Type | Notes |
|--------|------|-------|
| `ImpactJson` | `nvarchar(max) NULL` | Serialized `RecommendationImpact` |

---

## API Reference

### New Endpoints (Sprint 13C)

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/v1/copilot/drafts/generate` | Generate a new draft |
| `GET` | `/api/v1/copilot/drafts/{id}/versions` | Get version history for a draft |
| `POST` | `/api/v1/copilot/drafts/{id}/regenerate` | Regenerate (creates new version) |

### Existing Endpoints (Sprint 13A, unchanged)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/v1/copilot/drafts` | List all drafts for organization |
| `GET` | `/api/v1/copilot/drafts/{id}` | Get single draft |

### Generate Draft Request Body

```json
{
  "draftType": 0,
  "sourceType": 0,
  "persona": "AccreditationCoach",
  "recommendationId": "<guid>",
  "sourceEntityId": "<guid>",
  "additionalInstructions": "Focus on AAC standards."
}
```

---

## Frontend Components

| Component | Purpose |
|-----------|---------|
| `DraftCardComponent` | Compact card showing title, type badge, version, status, confidence. Actions: view, regenerate, versions |
| `DraftViewerComponent` | Full-width read-only markdown viewer with Source Intelligence panel. Actions: regenerate, history |
| `DraftVersionHistoryComponent` | Ordered list of all versions with status, date, generator, confidence. Immutable audit note |

### Store Additions (Sprint 13C)

```typescript
// New signals
activeDraft: Signal<AiDraft | null>
draftVersions: Signal<AiDraft[]>
draftCounts: ComputedSignal<{ total, capa, executiveBriefing, readiness, evidenceChecklist }>
latestDraftsByGroup: ComputedSignal<AiDraft[]>

// New actions
generateDraft(request: GenerateDraftRequest): void
generateDraftFromRecommendation(recommendation): void
regenerateDraft(draftId: string): void
loadDraftVersions(draftId: string): void
selectDraft(draft: AiDraft): void
clearActiveDraft(): void
```

---

## Extension Points for Sprint 13D

### 1. LLM Content Generator
Register `LlmDraftContentGenerator` in `Program.cs` to enable AI-enhanced section content.

### 2. Approval Card Framework
- `AiDraft.Approve()` and `AiDraft.MarkReviewed()` methods already exist
- Source Intelligence in `MetadataJson` provides approval context
- `DraftStatus.Approved` state ready for use

### 3. Audit Preparation Brief & Management Review Summary
Templates `AuditPreparationBrief` (4) and `ManagementReviewSummary` (5) are registered in `DraftType` enum. Implement `AuditPreparationBriefTemplate` and `ManagementReviewSummaryTemplate` using the same `DraftTemplateBase` pattern.

### 4. External Auditor Workspace (Sprint 14)
Drafts can be exported to an auditor workspace using:
- `MetadataJson` for full data provenance
- `GroupId` for version lineage
- `Content` as the auditor-facing document

---

## Sprint 13D Readiness Assessment

| Requirement | Status |
|-------------|--------|
| Drafts generated and persisted | ✅ Ready |
| Drafts versioned (immutable) | ✅ Ready |
| Source Intelligence in every draft | ✅ Ready |
| Organization scoping | ✅ Ready |
| Draft status lifecycle (Draft → Reviewed → Approved) | ✅ Ready |
| `Approve()` method on `AiDraft` entity | ✅ Ready |
| Advisory notices (no workflow mutations) | ✅ Ready |
| Recommendation → Draft mapping | ✅ Ready |
| Frontend viewer (read-only) | ✅ Ready |
| Approval Card UI | ⏳ Sprint 13D |
| Approval → Workflow Execution | ⏳ Sprint 13D |
| LLM-enhanced content | ⏳ Sprint 13D+ |

---

## Files Created/Modified

### New Files (Sprint 13C)

**Domain (7 files)**
- `Domain/Copilot/Enums/DraftType.cs`
- `Domain/Copilot/Enums/DraftSourceType.cs`
- `Domain/Copilot/DraftSourceIntelligence.cs`
- `Domain/Copilot/RecommendationImpact.cs`

**Application — Contracts (3 files)**
- `Application/Copilot/Contracts/IDraftTemplate.cs`
- `Application/Copilot/Contracts/IDraftContentGenerator.cs`
- `Application/Copilot/Contracts/IDraftGenerationService.cs`

**Application — Models (4 files)**
- `Application/Copilot/Models/DraftSection.cs`
- `Application/Copilot/Models/DraftContent.cs`
- `Application/Copilot/Models/DraftGenerationRequest.cs`
- `Application/Copilot/Models/DraftGenerationContext.cs`

**Application — Templates (6 files)**
- `Application/Copilot/Templates/DraftTemplateBase.cs`
- `Application/Copilot/Templates/DraftTemplateRegistry.cs`
- `Application/Copilot/Templates/CapaDraftTemplate.cs`
- `Application/Copilot/Templates/ExecutiveComplianceBriefingTemplate.cs`
- `Application/Copilot/Templates/ReadinessSummaryTemplate.cs`
- `Application/Copilot/Templates/EvidenceChecklistTemplate.cs`

**Application — Services (4 files)**
- `Application/Copilot/Services/DraftContentGenerators/TemplateDraftContentGenerator.cs`
- `Application/Copilot/Services/DraftContentGenerators/LlmDraftContentGenerator.cs`
- `Application/Copilot/Services/DraftGenerationService.cs`
- `Application/Copilot/Services/RecommendationToDraftMapper.cs`
- `Application/Copilot/Services/RecommendationImpactEstimator.cs`

**Infrastructure + Migration (3 files)**
- `Infrastructure/Copilot/Repositories/AiDraftRepository.cs` (updated)
- `Infrastructure/Copilot/EntityConfigurations/AiDraftConfiguration.cs` (updated)
- `Migrations/20260624140000_Sprint13C_DraftGeneration.cs`

**DTOs (2 new files)**
- `Application/Copilot/DTOs/AiDraftDto.cs` (updated)
- `Application/Copilot/DTOs/GenerateDraftRequestDto.cs`

**Frontend (9 files)**
- `features/copilot/models/ai-draft.model.ts` (updated)
- `features/copilot/services/copilot-api.service.ts` (updated)
- `features/copilot/state/copilot.store.ts` (updated)
- `features/copilot/components/draft-card/draft-card.component.ts`
- `features/copilot/components/draft-card/draft-card.component.html`
- `features/copilot/components/draft-viewer/draft-viewer.component.ts`
- `features/copilot/components/draft-viewer/draft-viewer.component.html`
- `features/copilot/components/draft-version-history/draft-version-history.component.ts`
- `features/copilot/components/draft-version-history/draft-version-history.component.html`

---

## Success Criteria Validation

| Criterion | Status |
|-----------|--------|
| ✓ Drafts generated from recommendations | ✅ `RecommendationToDraftMapper` + `generateDraftFromRecommendation()` |
| ✓ CAPA drafts supported | ✅ `CapaDraftTemplate` with 8 sections |
| ✓ Executive briefings supported | ✅ `ExecutiveComplianceBriefingTemplate` with 10 sections |
| ✓ Readiness summaries supported | ✅ `ReadinessSummaryTemplate` with 9 sections |
| ✓ Evidence checklists supported | ✅ `EvidenceChecklistTemplate` with 5 sections |
| ✓ Drafts versioned | ✅ `GroupId` + `GetVersionsByGroupAsync` |
| ✓ Drafts persisted | ✅ `AiDraftRepository.CreateAsync` |
| ✓ Drafts organization scoped | ✅ `OrganizationId` on all queries |
| ✓ No workflow execution | ✅ No write calls to external workflows |
| ✓ Source Intelligence in every draft | ✅ `DraftSourceIntelligence` in `MetadataJson` + HTML comment |
| ✓ Ready for Sprint 13D Approval Card Framework | ✅ `Approve()`, `MarkReviewed()`, `DraftStatus.Approved` ready |
