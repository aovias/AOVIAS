# Phase 4 — Governance Dashboard (Sprint 6)

**Sprint:** 6  
**Status:** Delivered (best-effort KPIs)

## Surface

WebApp `/app/governance` dashboard loads `GET /api/v1/analytics/governance` (Gateway merge) and falls back to `/governance/analytics/summary`.

## KPI tiles

| KPI | Source |
|-----|--------|
| Active committees | GovernanceService |
| Open action items | GovernanceService |
| Meetings this month | GovernanceService |
| Pending minutes approvals | GovernanceService |
| Effective documents | ComplianceService |
| Training overdue | 0 until LMS (honest placeholder) |
| Open incidents | ComplianceService |
| Open risks | ComplianceService |

## Notes

Training/LMS (CRP-016) remains open — tile reports `0` with source note. Role-homes/analytics can deep-link to `/app/governance` for QM personas.
