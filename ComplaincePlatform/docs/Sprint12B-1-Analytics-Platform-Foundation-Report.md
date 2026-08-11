# Sprint 12B-1 — Analytics Platform Foundation Report

**Sprint:** 12B-1 Analytics Platform Foundation  
**Predecessor:** [Sprint 12B-0 Analytics Stabilization](./Sprint12B-0-Analytics-Stabilization-Report.md)  
**Successor:** Sprint 12B-2 Executive Analytics Hub  
**Date:** June 2026  

---

## Executive Summary

Sprint 12B-1 introduces a **dedicated read-only Analytics Platform** separated from operational execution modules. All KPI computation delegates to the SSOT services established in 12B-0 — no duplicate formulas in controllers or Angular components.

**Delivered:**

- Analytics query orchestration layer (`IAnalyticsQueryService`)
- Daily analytics snapshot entity + background job
- Eight Gateway-proxied REST endpoints under `/api/v1/analytics/*`
- Analytics Hub shell at `/app/analytics` with KPI cards, summary panels, data tables, and deep links
- Trend dataset foundation (`TrendSeriesDto`) without chart libraries

---

## Deliverable 1 — Analytics Domain

Read-only analytics layer under `ComplianceService/Application/Analytics/`:

| Component | Purpose |
|-----------|---------|
| `IAnalyticsQueryService` / `AnalyticsQueryService` | Orchestrates all analytics views |
| `IAnalyticsSnapshotService` / `AnalyticsSnapshotService` | Daily rollup persistence |
| `AnalyticsDeepLinks` | Canonical routes to operational modules |
| `AnalyticsDtos.cs` | Overview, Executive, Operational, RCA, CAPA, SLA, Department, Trends DTOs |

No commands. No writes except append-only daily snapshots.

---

## Deliverable 2 — Analytics Query Layer

`AnalyticsQueryService` consumes **only** SSOT sources:

| Source | Used For |
|--------|----------|
| `IAnalyticsAggregationService` | Readiness, deficiency counts, governance KPIs |
| `IDeficiencyMetricsService` | CAPA/SLA scoped metrics |
| `ComplianceSnapshots` + `JourneyTrendBuilder` | Score/readiness/chapter/risk trends |
| `AnalyticsDailySnapshots` | Historical daily rollups |
| `IObligationQueryService` | Obligation readiness in department view |
| `ITaskQueryService` | Task throughput |
| `IEvidenceLibraryService` | Evidence pipeline |
| `ComplianceMaturityIndexService` | CMI (formula SSOT) |
| `IStageUnlockingService` | Stage progress label |

---

## Deliverable 3 — AnalyticsDailySnapshot

**Entity:** `Domain/AnalyticsDailySnapshot.cs`  
**Table:** `AnalyticsDailySnapshots`  
**Migration:** `20260624073428_Sprint12B1_AnalyticsFoundation`

| Field | Description |
|-------|-------------|
| `SnapshotDate` | Calendar day (UTC) |
| `OrganizationId`, `HospitalId`, `FrameworkId` | Scope |
| `ReadinessScore`, `ComplianceMaturityIndex` | Headline scores |
| `OpenDeficiencies`, `CriticalDeficiencies` | Deficiency KPIs |
| `CapaOpen`, `CapaEffective`, `CapaIneffective` | CAPA KPIs |
| `EscalationsOpen`, `SlaBreached` | SLA/escalation KPIs |
| `StageUnlockedCount`, `RecurringIssues` | Maturity + RCA |
| `EvidencePendingVerification`, `TaskBacklog` | Operational pipeline |

Unique index: `(OrganizationId, HospitalId, FrameworkId, SnapshotDate)` — append-only.

---

## Deliverable 4 — Analytics Snapshot Service

`AnalyticsSnapshotService.GenerateDailySnapshotAsync` delegates all metric fields to aggregation/metrics services before persist.

**Background job:** `AnalyticsDailySnapshotJob` — scheduled daily at **4:00 AM UTC** (after compliance snapshot recalc at 2 AM).

---

## Deliverable 5 — Analytics APIs

### ComplianceService (internal)

Route prefix: `api/v1/analytics`  
Auth: `ComplianceScope` + `[ValidateHospitalId]`

| Endpoint | DTO |
|----------|-----|
| `GET /overview` | `AnalyticsOverviewDto` |
| `GET /executive` | `ExecutiveAnalyticsDto` |
| `GET /operational` | `OperationalAnalyticsDto` |
| `GET /rca` | `RcaAnalyticsDto` |
| `GET /capa` | `CapaAnalyticsDto` |
| `GET /sla` | `SlaAnalyticsDto` |
| `GET /departments` | `DepartmentAnalyticsDto` |
| `GET /trends?metric=&from=&to=&granularity=` | `AnalyticsTrendsDto` |

Optional query: `frameworkId` (defaults to active journey framework).

### Gateway (portal-facing)

Mirrored at `api/v1/analytics/*` via MediatR proxy handlers.

---

## Deliverables 6–12 — Analytics DTOs

All DTOs in `Application/DTOs/AnalyticsDtos.cs`:

- **Overview** — headline KPI cards for Analytics Hub home
- **Executive** — readiness, department performance, CAPA/SLA governance, risk/escalation exposure
- **Operational** — deficiency funnel, task/evidence pipeline, assignment load
- **RCA** — root cause breakdowns by department/chapter/stage + trend datasets
- **CAPA** — effectiveness, closure time, department performance (via `CapaQueryService`)
- **SLA** — health, breach/escalation rates, department compliance (via `EscalationService`)
- **Departments** — per-department scorecard rows with deep links

---

## Deliverable 13 — Analytics Hub Shell

**Route:** `/app/analytics` (+ `/app/analytics/{section}`)  
**Navigation:** Sidebar **Analytics Hub** (replaces control-center link for analytics)

Sections: Overview, Executive, Operational, RCA, CAPA, SLA, Departments

**UI scope (Phase 1):** KPI cards, summary panels, data tables — **no chart libraries**.

Files:

```
WebApp/src/app/features/analytics/
  routes.ts
  models/analytics.model.ts
  services/analytics-api.service.ts
  pages/analytics-hub.component.ts
```

---

## Deliverable 14 — Deep Link Architecture

Every actionable insight includes `AnalyticsDeepLinkDto`:

| Insight | Deep Link Target |
|---------|------------------|
| Open deficiencies | `/app/audit/corrective-actions` |
| Critical / SLA / recurring / escalated | DCC with filter query param |
| Evidence verification backlog | `/app/evidence?view=pendingReview` |
| Tasks | `/app/tasks` |
| Findings | `/app/audit/findings` |
| Readiness / CMI | `/app/journey`, `/app/compliance-maturity` |
| Department scorecard row | DCC with `department` query param |

Analytics remains observational; execution stays in operational modules.

---

## Deliverable 15 — Trend Readiness Layer

`TrendSeriesDto` supports `Daily | Weekly | Monthly | Quarterly` granularity.

Sources:

- Live: `ComplianceSnapshots` via `JourneyTrendBuilder` (readiness, overall score, chapter progress, risk)
- Historical: `AnalyticsDailySnapshots` (open deficiencies, CMI, SLA breaches, recurring issues)

Empty series returned when data unavailable — no fake points.

---

## Deliverable 16 — Validation Report

| Check | Result |
|-------|--------|
| `dotnet build ComplianceService` | Pass |
| `dotnet build Gateway` | Pass |
| `dotnet test ComplianceService.Tests` | 188/188 pass |
| EF migration created | `Sprint12B1_AnalyticsFoundation` |
| KPI duplication in controllers | None — delegates to query service |
| KPI duplication in Angular | None — consumes API only |
| Organization scoping | JWT `organization_id` + `hospital_id` enforced |
| Deep links present on KPI cards and breakdown rows | Yes |

---

## Sprint 12B-2 Readiness

| Ready | Notes |
|-------|-------|
| Analytics API scaffold | All 8 endpoints operational |
| Analytics Hub shell | Menu + section navigation |
| Daily snapshot foundation | Job scheduled; trends populate after first run |
| SSOT preserved | All metrics delegate to 12B-0 services |

| Deferred to 12B-2+ | Notes |
|--------------------|-------|
| Executive charts/gauges | Use trend datasets from `/analytics/trends` |
| Chart library integration | Datasets ready; UI tables only in 12B-1 |
| Analytics fact tables (dimensional) | Daily snapshot is first rollup; expand in 12B-2 |

---

## Success Criteria

| Criterion | Status |
|-----------|--------|
| Dedicated Analytics Domain exists | ✅ |
| Analytics Hub menu exists | ✅ |
| Analytics APIs operational | ✅ |
| AnalyticsDailySnapshot implemented | ✅ |
| Aggregation Service reused | ✅ |
| No duplicate KPI calculations | ✅ |
| Organization-scoped analytics | ✅ |
| Deep links to operational modules | ✅ |
| Historical analytics foundation | ✅ |
| Ready for Sprint 12B-2 | ✅ |

---

**End of Sprint 12B-1 — Analytics Platform Foundation Report**
