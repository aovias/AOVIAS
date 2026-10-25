# Phase 3 — Role Analytics (Sprint 6)

**Sprint:** 6  
**Status:** Delivered (best-effort)  
**Endpoint:** `GET /api/v1/analytics/role-engagement`

## ComplianceService

- Controller: `Controllers/V1/AnalyticsController.GetRoleEngagement`  
- Service: `IRoleExperienceService.GetEngagementAsync` → pack resolve + operational/CAPA analytics  
- DTO: `RoleEngagementDto` / `RoleEngagementCountersDto`

### Counters (best-effort mapping)

| Counter | Source |
|---------|--------|
| TasksCompleted | `OperationalAnalytics.TaskThroughput.Completed` |
| ReviewsCompleted | `EvidencePipeline.Approved` |
| ReviewsPending | max(VerificationBacklog, EvidencePipeline.PendingReview) |
| CapaResolutions | `DeficiencyFunnel.Closed` |
| CapasEffective | `CapaAnalytics.Metrics.EffectiveCapas` |
| AuditsCompleted | `AuditSessionsClosedOrSubmitted` |
| OverdueTasks / OverdueCapas | `OverdueWork` |

Failures return zeroed counters (same posture as attention counts).  
`Source` = `analytics-operational-best-effort`.

## Gateway

- `AnalyticsController.GetRoleEngagement` → MediatR `GetAnalyticsRoleEngagementQuery` → `IProxyService` to ComplianceService  
- Query string (e.g. `frameworkId`) forwarded

## Relation to Copilot usage-metrics

Copilot `GET /copilot/usage-metrics` remains assistant-activity only. Role pack engagement is a **separate** analytics endpoint so domain services stay broker-agnostic and reuse the analytics SSOT.

## Auth / envelope

- Policy: `ComplianceScope`  
- Hospital validation via `[ValidateHospitalId]`  
- Success envelope: `ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, …)`
