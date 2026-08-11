# Phase 4 — Governance Analytics (Sprint 6)

**Sprint:** 6  
**Status:** Delivered

## Endpoints

| Layer | Route | Payload |
|-------|-------|---------|
| GovernanceService | `GET /api/v1/governance/analytics/summary` | Extended committee/meeting/action counters |
| GovernanceService | `GET /api/v1/governance/analytics/kpis` | Governance KPI DTO |
| ComplianceService | `GET /api/v1/analytics/governance` | Docs / incidents / risks enrichment |
| Gateway | `GET /api/v1/analytics/governance` | **Merged** KPIs for WebApp |

## Merge behavior

Gateway `GetAnalyticsGovernanceQueryHandler` combines Governance KPI counts with Compliance domain enrichment; failures on one side yield zeros for that side rather than failing the whole response when the other succeeds.
