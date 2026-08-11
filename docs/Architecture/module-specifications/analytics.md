# Module Specification: Analytics Hub

[← Module Catalog](../04-Module-Catalog.md) · [Analytics Architecture](../17-Analytics-Architecture.md)

**Implementation status:** **Complete** (~85%)  
**Route:** `/app/analytics`, `/app/analytics/:section`

## Purpose

Cross-domain KPIs, trends, and readiness analytics for leadership and quality teams.

## Owns (write model)

- `AnalyticsDailySnapshot`

## Does NOT Own

- Operational mutations
- Copilot effectiveness (separate Copilot analytics)

## Key APIs

- Gateway `/analytics/*`
- ComplianceService `AnalyticsController`

## Sprint

- 12B Analytics Platform Foundation

## Gaps

- Duplicate KPI presentation with Dashboard/Maturity (TD-M05)
- BI export not implemented
