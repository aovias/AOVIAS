# Phase 2 — Operational Analytics (Sprint 6)

**Sprint:** 6  
**Status:** Delivered  
**Principle:** Extend `GET /api/v1/analytics/operational` — reuse `CapaQueryService` / SLA metrics; Gateway already proxies.

## API

| Layer | Route |
|-------|-------|
| ComplianceService | `GET /api/v1/analytics/operational?frameworkId=` |
| Gateway | `GET /api/v1/analytics/operational` (MediatR proxy) |

Success envelope: `ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, …)`.

## Operational payload (Sprint 6 additions)

Built in `AnalyticsQueryService.GetOperationalAsync` on top of existing funnel / tasks / evidence:

| Field | Source |
|-------|--------|
| `capaAging` / `averageOpenCapaAgeDays` | `IDeficiencyMetricsService.GetCapaMetricsAsync` → `CapaQueryService` |
| `averageResolutionHours` | SLA escalation metrics |
| `averageResolutionDays` | CAPA average closure days |
| `auditCompletionRate` / session counts | `AuditSessions` (Closed + Submitted / total) |
| `departmentPerformance` | CAPA department performance labels |
| `slaComplianceRate` / `slaHealthScore` | `100 - breachRate%`, governance SLA health |
| `overdueWork` | Overdue CAPAs, tasks, deficiencies + dept overdue CAPAs |

Existing fields retained: deficiency funnel, task throughput, evidence pipeline, root-cause trend, deep links.

## WebApp

Analytics Hub **Operational** section (`/app/analytics/operational`):

- KPI tiles for resolution, audit completion, SLA, overdue  
- CAPA aging bars  
- Department performance / overdue tables  
- Client CSV export (`Export CSV`)

## Exit criteria

- Single operational endpoint exposes aging + resolution + audit completion + SLA + overdue  
- WebApp renders and can export without a separate analytics microservice  
