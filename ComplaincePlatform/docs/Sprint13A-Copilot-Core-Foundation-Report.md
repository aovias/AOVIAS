# Sprint 13A – Copilot Core Foundation Report

**Date:** June 24, 2026
**Sprint:** 13A
**Status:** Completed
**Preceded by:** Sprint 12D – Copilot Readiness & AI Responsibility Design
**Followed by:** Sprint 13B – Recommendation Engine

---

## Executive Summary

Sprint 13A establishes the foundational bounded context for the PRAMAN Compliance Copilot. It creates all domain entities, persona routing infrastructure, application service contracts, repository layer, REST API surface, frontend models and state store, and the database migration — without implementing any recommendation generation, draft generation, or approval workflows. Those capabilities are engineered in Sprints 13B–13D atop this foundation.

**Critical constraints satisfied:**
- No recommendation generation implemented
- No draft generation implemented
- No approval cards implemented
- No autonomous actions
- No modifications to existing compliance workflows
- No AI write access

---

## Architecture Overview

The Copilot domain is a **dedicated bounded context** inside `ComplianceService`, parallel to the existing `Application/AI/` and `Application/Analytics/` trees. It deliberately does not replace or extend those subsystems at this stage.

```
ComplianceService/
├── Domain/Copilot/                     ← Entities, enums, context model
├── Application/Copilot/
│   ├── Contracts/                      ← Repository and service interfaces
│   ├── DTOs/                           ← API read models
│   └── Services/                       ← Persona router + stub services
└── Infrastructure/Copilot/
    ├── EntityConfigurations/           ← EF Core IEntityTypeConfiguration
    └── Repositories/                   ← Repository implementations

Gateway/
├── Application/Queries/CopilotQueries.cs
├── Application/Handlers/CopilotQueryHandlers.cs
└── Controllers/CopilotController.cs

WebApp/src/app/features/copilot/
├── models/
├── services/
└── state/
```

### Separation from existing subsystems

| Subsystem | Relationship |
|-----------|-------------|
| `Application/AI/` (Sprint 12C) | Provides `AIContextBundle` (read-only tool context). `CopilotContext` can compose it later in 13B. |
| `Application/Analytics/` (Sprint 12B-1) | Analytics data is included in `CopilotContext.AnalyticsSummary` as a string; no direct dependency yet. |
| Legacy `CopilotExplanation*` services | Remain untouched in `Application/Services/`. Separate rule-based explainability layer. |

---

## Phase 1 – Domain Layer

### Entities

#### `CopilotRecommendation` (`Domain/Copilot/CopilotRecommendation.cs`)

Extends `AggregateRoot`. Organization-scoped from day one.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | `ValueGeneratedNever()` |
| `OrganizationId` | `Guid` | Tenant isolation; indexed |
| `RecommendationType` | `string(100)` | e.g. "DeficiencyRemediation", "EvidenceGap" |
| `Persona` | `string(100)` | Persona ID string from `CopilotPersona` enum |
| `Priority` | `int` | Higher = more urgent |
| `Title` | `string(500)` | Short recommendation heading |
| `Description` | `string(4000)` | Full description |
| `Rationale` | `string(4000)` | Why this recommendation was generated |
| `ExpectedOutcome` | `string(2000)` | What accepting it achieves |
| `RiskIfIgnored` | `string(2000)` | Compliance risk if dismissed |
| `SourceReferences` | `string?(4000)` | Linked entity IDs / NABH references |
| `Status` | `RecommendationStatus` | Stored as `int` |
| `GeneratedAtUtc` | `DateTime` | Indexed |
| `AcceptedAtUtc` | `DateTime?` | Set on `Accept()` |
| `DismissedAtUtc` | `DateTime?` | Set on `Dismiss()` |

**State machine:** `Proposed → Accepted | Dismissed | Expired`

#### `AiDraft` (`Domain/Copilot/AiDraft.cs`)

Extends `AggregateRoot`. Supports incremental versioning.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | `ValueGeneratedNever()` |
| `OrganizationId` | `Guid` | Tenant isolation; indexed |
| `DraftType` | `string(100)` | e.g. "CapaDraft", "EvidenceNarrative", "PolicyText" |
| `Persona` | `string(100)` | Generating persona |
| `Title` | `string(500)` | Draft heading |
| `Content` | `nvarchar(max)` | Full draft text |
| `SourceContext` | `nvarchar(max)?` | Context snapshot at generation time |
| `Status` | `DraftStatus` | Stored as `int` |
| `Version` | `int` | Starts at 1; incremented by `UpdateContent()` |
| `CreatedUtc` | `DateTime` | Indexed |
| `UpdatedUtc` | `DateTime` | Updated on each mutation |

**State machine:** `Draft → Reviewed → Approved | Archived`

### Enums

| Enum | Values |
|------|--------|
| `CopilotPersona` | `AccreditationCoach`, `QualityManagerAssistant`, `EvidenceAdvisor`, `CapaAdvisor`, `ExecutiveAdvisor`, `DepartmentHeadAssistant`, `AuditorAssistant` |
| `RecommendationStatus` | `Proposed=0`, `Accepted=1`, `Dismissed=2`, `Expired=3` |
| `DraftStatus` | `Draft=0`, `Reviewed=1`, `Approved=2`, `Archived=3` |

### `CopilotContext` (`Domain/Copilot/CopilotContext.cs`)

Plain sealed class (not `AggregateRoot`) — represents the runtime state of a Copilot session.

| Field | Purpose |
|-------|---------|
| `OrganizationId` | Tenant scoping |
| `PageContext` | Matches `AiPageContextService` strings from frontend (Sprint 12C) |
| `FocusedEntityId / FocusedEntityType` | Entity in focus |
| `ConversationSummary` | Rolling conversation thread summary |
| `RecentEntityRefs` | Last 8 entity refs (same limit as Sprint 12C AI context) |
| `ActivePersona` | Resolved persona |
| `PersonaOverride` | Manual override (null = auto-route) |
| `CurrentStage` | Maturity stage label |
| `ReadinessScore` | Latest readiness % |
| `AnalyticsSummary` | Condensed analytics string for context injection |

---

## Phase 2 – Application Contracts

All interfaces live under `Application/Copilot/Contracts/`.

### Repository Interfaces

**`ICopilotRecommendationRepository`**
- `CreateAsync`, `GetAsync`, `SearchAsync` (org + status + persona + pagination)
- `AcceptAsync`, `DismissAsync`, `ExpireAsync` (state transitions, persistence)
- `SaveChangesAsync`

**`IAiDraftRepository`**
- `CreateAsync`, `GetAsync`, `SearchAsync` (org + status + persona + type + pagination)
- `UpdateAsync`, `ArchiveAsync`
- `GetVersionHistoryAsync` (by org + title)
- `SaveChangesAsync`

### Service Interfaces

**`ICopilotPersonaRouter`** — `DeterminePersona(CopilotContext): CopilotPersona`

**`ICopilotPersonaService`** — `GetActivePersona`, `OverridePersona`, `ResetPersona`, `GetPersonaCatalog`

**`ICopilotRecommendationService`** — `GetRecommendationsAsync`, `GetRecommendationAsync`, `AcceptAsync`, `DismissAsync`

**`ICopilotDraftService`** — `GetDraftsAsync`, `GetDraftAsync`, `UpdateDraftAsync`, `ArchiveDraftAsync`

---

## Phase 3 – Persona Routing

### Decision Hierarchy (`CopilotPersonaRouter.cs`)

```
1. Manual Override (context.PersonaOverride is set)
   ↓
2. Page Context Map
   ├── Dcc / DeficiencyCommandCenter  → CapaAdvisor
   ├── ComplianceMaturity             → AccreditationCoach
   ├── Journey / Evidence / Binder    → EvidenceAdvisor
   ├── ReviewQueue / Audit            → AuditorAssistant
   └── AnalyticsHub / Analytics       → ExecutiveAdvisor
   ↓
3. Entity Type Detection
   ├── Deficiency / CAPA              → CapaAdvisor
   ├── Evidence                       → EvidenceAdvisor
   ├── Finding / AuditFinding         → AuditorAssistant
   └── ObjectiveElement               → AccreditationCoach
   ↓
4. Default: AccreditationCoach
```

Page context string values match `AiPageContextService` from Sprint 12C frontend exactly.

---

## Phase 4 – Infrastructure Layer

### EF Configurations

`CopilotRecommendationConfiguration` and `AiDraftConfiguration` implement `IEntityTypeConfiguration<T>`. Both registered via `builder.ApplyConfiguration(...)` in `ComplianceDbContext.OnModelCreating`.

### Repositories

Both extend `Repository<T>` base class (hospital filter via reflection). Added `DbSet<CopilotRecommendation>` and `DbSet<AiDraft>` to `ComplianceDbContext`.

---

## Phase 5 – Database Migration

**File:** `Migrations/20260624120000_Sprint13A_CopilotCore.cs`
**Namespace:** `ComplianceService.Migrations`

### Tables Created

**`CopilotRecommendations`**

| Column | Type | Nullable |
|--------|------|----------|
| Id | uniqueidentifier (PK) | No |
| OrganizationId | uniqueidentifier | No |
| RecommendationType | nvarchar(100) | No |
| Persona | nvarchar(100) | No |
| Priority | int | No |
| Title | nvarchar(500) | No |
| Description | nvarchar(4000) | No |
| Rationale | nvarchar(4000) | No |
| ExpectedOutcome | nvarchar(2000) | No |
| RiskIfIgnored | nvarchar(2000) | No |
| SourceReferences | nvarchar(4000) | Yes |
| Status | int | No |
| GeneratedAtUtc | datetime2 | No |
| AcceptedAtUtc | datetime2 | Yes |
| DismissedAtUtc | datetime2 | Yes |

**`AiDrafts`**

| Column | Type | Nullable |
|--------|------|----------|
| Id | uniqueidentifier (PK) | No |
| OrganizationId | uniqueidentifier | No |
| DraftType | nvarchar(100) | No |
| Persona | nvarchar(100) | No |
| Title | nvarchar(500) | No |
| Content | nvarchar(max) | No |
| SourceContext | nvarchar(max) | Yes |
| Status | int | No |
| Version | int | No |
| CreatedUtc | datetime2 | No |
| UpdatedUtc | datetime2 | No |

### Indexes Created

| Table | Index | Column |
|-------|-------|--------|
| CopilotRecommendations | IX_CopilotRecommendations_OrganizationId | OrganizationId |
| CopilotRecommendations | IX_CopilotRecommendations_Status | Status |
| CopilotRecommendations | IX_CopilotRecommendations_Persona | Persona |
| CopilotRecommendations | IX_CopilotRecommendations_GeneratedAtUtc | GeneratedAtUtc |
| AiDrafts | IX_AiDrafts_OrganizationId | OrganizationId |
| AiDrafts | IX_AiDrafts_Status | Status |
| AiDrafts | IX_AiDrafts_Persona | Persona |
| AiDrafts | IX_AiDrafts_CreatedUtc | CreatedUtc |

---

## Phase 6 – API Layer

### ComplianceService Controllers (`Controllers/V1/Copilot/`)

All follow existing patterns: `[ApiController]`, `[ApiVersion("1.0")]`, `[Authorize(Policy = ComplianceScope)]`, `[ValidateHospitalId]`, `ApiResponse<T>` envelope.

| Controller | Routes | Phase |
|-----------|--------|-------|
| `CopilotRecommendationsController` | `GET /api/v1/copilot/recommendations` | 13A read |
| | `GET /api/v1/copilot/recommendations/{id}` | 13A read |
| `CopilotDraftsController` | `GET /api/v1/copilot/drafts` | 13A read |
| | `GET /api/v1/copilot/drafts/{id}` | 13A read |
| `CopilotPersonasController` | `GET /api/v1/copilot/personas` | 13A |
| | `GET /api/v1/copilot/personas/active` | 13A |

### Gateway Controller (`Gateway/Controllers/CopilotController.cs`)

Proxies all six routes to ComplianceService via MediatR + `IProxyService`. Pattern follows `AnalyticsController`.

Six query records in `CopilotQueries.cs` + six handler classes in `CopilotQueryHandlers.cs`.

---

## Phase 7 – DI Registration

Added to `ComplianceService/Program.cs` after the Sprint 12C AI block:

```csharp
// Sprint 13A – Copilot Core Foundation
builder.Services.AddScoped<ICopilotRecommendationRepository, CopilotRecommendationRepository>();
builder.Services.AddScoped<IAiDraftRepository, AiDraftRepository>();
builder.Services.AddScoped<ICopilotPersonaRouter, CopilotPersonaRouter>();
builder.Services.AddScoped<ICopilotPersonaService, CopilotPersonaService>();
builder.Services.AddScoped<ICopilotRecommendationService, CopilotRecommendationService>();
builder.Services.AddScoped<ICopilotDraftService, CopilotDraftService>();
```

---

## Phase 8 – Frontend Foundation

**Location:** `WebApp/src/app/features/copilot/`

### Models

| File | Contents |
|------|----------|
| `copilot-persona.model.ts` | `CopilotPersona` enum, `PersonaInfo` interface, `PERSONA_DISPLAY_NAMES` map |
| `copilot-recommendation.model.ts` | `RecommendationStatus` enum, `CopilotRecommendation` interface |
| `ai-draft.model.ts` | `DraftStatus` enum, `AiDraft` interface |
| `copilot-context.model.ts` | `CopilotContext` interface, `CopilotContextRequest` |

### Service (`copilot-api.service.ts`)

Extends `BaseApiService`. Exposes:
- `getRecommendations()`, `getRecommendation(id)`
- `getDrafts()`, `getDraft(id)`
- `getPersonas()`, `getActivePersona(params?)`

### Store (`copilot.store.ts`)

Angular signals store (`providedIn: 'root'`):

| Signal | Type |
|--------|------|
| `activePersona` | `signal<CopilotPersona>` |
| `personaOverride` | `signal<CopilotPersona \| null>` |
| `personaCatalog` | `signal<PersonaInfo[]>` |
| `recommendations` | `signal<CopilotRecommendation[]>` |
| `drafts` | `signal<AiDraft[]>` |
| `copilotContext` | `signal<CopilotContext \| null>` |
| `loading` | `signal<{...}>` |
| `error` | `signal<string \| null>` |

Computed: `proposedRecommendations`, `activeDrafts`, `effectivePersona`.

---

## Validation Checklist

| Item | Status |
|------|--------|
| Copilot bounded context created | ✓ |
| `CopilotRecommendation` entity created | ✓ |
| `AiDraft` entity created | ✓ |
| `CopilotPersona` enum (7 values) | ✓ |
| `RecommendationStatus` enum | ✓ |
| `DraftStatus` enum | ✓ |
| `CopilotContext` model | ✓ |
| Persona router (rule-based, 4-tier hierarchy) | ✓ |
| All 6 repository/service contracts | ✓ |
| `ICopilotRecommendationRepository` implementation | ✓ |
| `IAiDraftRepository` implementation | ✓ |
| EF Core configurations for both tables | ✓ |
| `ComplianceDbContext` updated | ✓ |
| Migration `Sprint13A_CopilotCore` | ✓ |
| 6 API endpoints on ComplianceService | ✓ |
| 6 API endpoints proxied via Gateway | ✓ |
| DI registration in `Program.cs` | ✓ |
| Frontend models (4 files) | ✓ |
| Frontend API service | ✓ |
| Frontend signals store | ✓ |
| No workflow changes | ✓ |
| No autonomous behavior | ✓ |
| All entities org-scoped from day one | ✓ |

---

## Sprint 13B Readiness Assessment

Sprint 13B adds the Recommendation Engine on top of this foundation.

### What 13B can build directly on

| 13A Deliverable | 13B Use |
|-----------------|---------|
| `CopilotRecommendation` entity + `Create()` factory | Recommendation generation writes via this factory |
| `ICopilotRecommendationRepository.CreateAsync` | Persists generated recommendations |
| `ICopilotRecommendationService` contract | Add generation logic inside the stub service |
| `CopilotContext` model | Pass as input to LLM prompt builder |
| Persona router | Determines recommendation style and tone per persona |
| `AIContextBundle` (Sprint 12C) | Compose into `CopilotContext` for rich context |
| Analytics SSOT (Sprint 12B-1) | Feed into recommendation rationale |
| Frontend `CopilotStore.recommendations` signal | Bind to recommendation widget without store changes |
| `GET /api/v1/copilot/recommendations` | Frontend already wired to call this |

### Extension points marked in code

- `CopilotRecommendationService.GetRecommendationsAsync` — trigger generation when no fresh recommendations exist
- `CopilotRecommendationService.AcceptAsync` — trigger follow-up recommendation refresh
- `CopilotRecommendationService.DismissAsync` — log dismissal signal for feedback loop
- `CopilotDraftService.UpdateDraftAsync` — collaborative editing, diff tracking

### Recommended 13B interfaces to add

```csharp
// Sprint 13B additions
IRecommendationGenerationEngine
IRecommendationContextBuilder
IRecommendationRanker
```

### Frontend readiness

- `CopilotStore.loadRecommendations()` — already calls API
- `proposedRecommendations` computed signal — ready to bind to recommendation widget
- `effectivePersona` computed — available for LLM prompt construction
- No widget code yet — Sprint 13B adds `RecommendationCardComponent`

---

## File Inventory

### New Files (39)

| Path | Description |
|------|-------------|
| `ComplianceService/Domain/Copilot/CopilotPersona.cs` | Persona enum |
| `ComplianceService/Domain/Copilot/Enums/RecommendationStatus.cs` | Recommendation status enum |
| `ComplianceService/Domain/Copilot/Enums/DraftStatus.cs` | Draft status enum |
| `ComplianceService/Domain/Copilot/CopilotRecommendation.cs` | Recommendation aggregate |
| `ComplianceService/Domain/Copilot/AiDraft.cs` | Draft aggregate |
| `ComplianceService/Domain/Copilot/CopilotContext.cs` | Session context model |
| `ComplianceService/Application/Copilot/Contracts/ICopilotRecommendationRepository.cs` | Repository contract |
| `ComplianceService/Application/Copilot/Contracts/IAiDraftRepository.cs` | Repository contract |
| `ComplianceService/Application/Copilot/Contracts/ICopilotPersonaRouter.cs` | Router contract |
| `ComplianceService/Application/Copilot/Contracts/ICopilotRecommendationService.cs` | Service contract |
| `ComplianceService/Application/Copilot/Contracts/ICopilotDraftService.cs` | Service contract |
| `ComplianceService/Application/Copilot/Contracts/ICopilotPersonaService.cs` | Service contract |
| `ComplianceService/Application/Copilot/DTOs/CopilotRecommendationDto.cs` | Read DTO |
| `ComplianceService/Application/Copilot/DTOs/AiDraftDto.cs` | Read DTO |
| `ComplianceService/Application/Copilot/DTOs/CopilotContextDto.cs` | Request DTO |
| `ComplianceService/Application/Copilot/DTOs/PersonaResponseDto.cs` | Read DTO |
| `ComplianceService/Application/Copilot/Services/CopilotPersonaRouter.cs` | Rule-based router impl |
| `ComplianceService/Application/Copilot/Services/CopilotPersonaService.cs` | Persona service impl |
| `ComplianceService/Application/Copilot/Services/CopilotRecommendationService.cs` | Foundation stub impl |
| `ComplianceService/Application/Copilot/Services/CopilotDraftService.cs` | Foundation stub impl |
| `ComplianceService/Infrastructure/Copilot/EntityConfigurations/CopilotRecommendationConfiguration.cs` | EF config |
| `ComplianceService/Infrastructure/Copilot/EntityConfigurations/AiDraftConfiguration.cs` | EF config |
| `ComplianceService/Infrastructure/Copilot/Repositories/CopilotRecommendationRepository.cs` | Repository impl |
| `ComplianceService/Infrastructure/Copilot/Repositories/AiDraftRepository.cs` | Repository impl |
| `ComplianceService/Migrations/20260624120000_Sprint13A_CopilotCore.cs` | EF migration |
| `ComplianceService/Controllers/V1/Copilot/CopilotRecommendationsController.cs` | REST controller |
| `ComplianceService/Controllers/V1/Copilot/CopilotDraftsController.cs` | REST controller |
| `ComplianceService/Controllers/V1/Copilot/CopilotPersonasController.cs` | REST controller |
| `Gateway/Application/Queries/CopilotQueries.cs` | MediatR queries |
| `Gateway/Application/Handlers/CopilotQueryHandlers.cs` | MediatR handlers |
| `Gateway/Controllers/CopilotController.cs` | Gateway proxy controller |
| `WebApp/.../features/copilot/models/copilot-persona.model.ts` | Frontend model |
| `WebApp/.../features/copilot/models/copilot-recommendation.model.ts` | Frontend model |
| `WebApp/.../features/copilot/models/ai-draft.model.ts` | Frontend model |
| `WebApp/.../features/copilot/models/copilot-context.model.ts` | Frontend model |
| `WebApp/.../features/copilot/services/copilot-api.service.ts` | Frontend API service |
| `WebApp/.../features/copilot/state/copilot.store.ts` | Frontend signals store |
| `ComplaincePlatform/docs/Sprint13A-Copilot-Core-Foundation-Report.md` | This document |

### Modified Files (2)

| Path | Change |
|------|--------|
| `ComplianceService/Infrastructure/Persistence/ComplianceDbContext.cs` | Added `DbSet<CopilotRecommendation>`, `DbSet<AiDraft>`, two `ApplyConfiguration` calls |
| `ComplianceService/Program.cs` | Added 6 Scoped DI registrations |

---

*Sprint 13A complete. Sprint 13B Recommendation Engine can begin immediately.*
