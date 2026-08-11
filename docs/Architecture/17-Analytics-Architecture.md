# Analytics Architecture

[← README](./README.md) · [Module spec](./module-specifications/analytics.md)

---

## Surfaces

| Surface | Route | SSOT |
|---------|-------|------|
| Analytics Hub | `/app/analytics` | `AnalyticsDailySnapshot` + live reads |
| Dashboard | `/app/dashboard` | Program overview snapshot |
| Compliance Maturity | `/app/compliance-maturity` | Maturity stage state |
| Copilot Analytics | `/app/copilot` (tab) | Copilot effectiveness tables |

**Boundary:** Copilot analytics measures **Copilot effectiveness**, not hospital operational SSOT (TD-H04).

---

## KPIs (Analytics Hub)

Derived from `AnalyticsDailySnapshot` and related read services:

- Readiness score
- Compliance maturity index
- Open/critical deficiencies
- CAPA open/effective/ineffective
- Escalations open

Exact KPI set: see Analytics controllers and Sprint 12B reports.

---

## Snapshot Strategy

| Snapshot | Cadence | Job |
|----------|---------|-----|
| Compliance hospital snapshot | On-demand + materialization | Snapshot services |
| Analytics daily | Daily 4 AM UTC | `analytics-daily-snapshot` Hangfire |
| Copilot effectiveness | On trends API call | **Planned:** nightly job (TD-M02) |

---

## Aggregation

- SQL aggregations in analytics services.
- Copilot: `CopilotAnalyticsService`, `RecommendationImpactAnalysisService`.

---

## Trend Generation

- `GET /analytics/trends` (compliance analytics).
- `GET /copilot/analytics/trends?days=N` (Copilot).

---

## Read-Only Rule

Analytics APIs do not mutate operational domains ([06-Business-Rules.md](./06-Business-Rules.md)).

---

## Future BI

- Export to Power BI / warehouse (**planned**).
- Consultant benchmarking dashboards (**roadmap**).
- Unified KPI SSOT across Dashboard/Maturity/Hub (**TD-M05**).
