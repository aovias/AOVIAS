# Phase 3 — Role Experience Framework (Sprint 1)

**Sprint:** 1  
**Status:** Delivered  
**Principle:** Extend `PramanDashboardCatalog`, `PersonaExperienceRegistry` / RecommendationEngine, and WebApp dashboard + sidebar. **No** new RoleExperience microservice. Staff / Doctor / Nurse packs deferred.

## Model (SSOT)

Code-first packs live in `ComplianceService/Application/RoleExperience/RoleExperiencePackRegistry.cs`.

| PackId | Persona | Default home | ApplicationRoles / aliases |
|--------|---------|--------------|----------------------------|
| `hospital-admin` | ExecutiveAdvisor | `/app/dashboard?persona=hospital-admin` | ORG_ADMIN, GROUP_ADMIN, SUPER_ADMIN, HospitalOwner, HospitalAdmin |
| `quality-manager` | QualityManagerAssistant | `/app/dashboard?persona=quality-manager` | MANAGER, Quality Manager |
| `department-head` | DepartmentHeadAssistant | `/app/dashboard?persona=department-head` | Department Head, DEPARTMENT_HEAD (checked before MANAGER) |
| `consultant` | ExecutiveAdvisor | `/app/dashboard?persona=consultant` | COMPLIANCE_VIEWER, REGULATORY_ADMIN, AUTHORITY |
| `auditor` | AuditorAssistant | `/app/auditor/dashboard` | EXTERNAL_AUDITOR, REGULATOR |

Each pack carries: widget allow-list, nav section ids, and default quick actions.

## APIs

### ComplianceService

| Method | Route |
|--------|--------|
| GET | `/api/v1/role-experience/home` |

Response shape:

```json
{
  "packId": "quality-manager",
  "persona": "QualityManagerAssistant",
  "defaultHomeRoute": "/app/dashboard?persona=quality-manager",
  "priorities": [{ "title": "...", "rationale": "...", "href": "...", "severity": "High" }],
  "attentionCounts": {
    "overdueTasks": 0,
    "pendingReviews": 0,
    "criticalCapas": 0,
    "upcomingAudits": 0
  },
  "quickActions": [{ "label": "...", "href": "..." }]
}
```

Priorities are top 3–5 recommendations for the pack persona (`ICopilotRecommendationService.GetByPersonaAsync`). Attention counts are best-effort from analytics (zeros on failure).

### Gateway

| Method | Route |
|--------|--------|
| GET | `/api/v1/role-experience/home` |

Thin `IProxyService` proxy (`Gateway/Controllers/RoleExperienceController.cs`).

## Persona defaults

When Copilot persona query / page context is missing:

1. `CopilotPersonaRouter` uses `CopilotContext.UserRoles` → `RoleExperiencePackRegistry`.
2. `CopilotExperienceController` workspace resolves persona from JWT roles when `persona` query is absent.
3. `DashboardContextBuilder` injects `role-pack:{packId}` into dashboard context permissions.

## Catalog enrichment

`PramanDashboardCatalog` tags widgets/sections with `compliance.api` + `role-pack:*` permissions and `Visibility = Conditional`.  
`DashboardProviderRegistry` requires all base permissions and **any** matching role-pack tag (when context has pack tags).

## Recommendation stubs

Five role-specific `IRecommendationRule` stubs under `Application/Copilot/Rules/RoleExperience/`:

- OwnerExecutiveAttentionRule  
- QualityManagerDailyPrioritiesRule  
- DepartmentHeadTaskBacklogRule  
- ConsultantPortfolioRiskRule  
- AuditorVerificationQueueRule  

## Tests

`ComplianceService.Tests/RoleExperiencePackRegistryTests.cs` — pack resolution, priority, aliases, permission tags.
