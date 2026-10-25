# Phase 3 — Hospital Owner Experience (Sprint 2)

**Sprint:** 2  
**Status:** Delivered  
**Principle:** Compose an executive workspace from existing dashboard widgets, analytics APIs, and Persona Intelligence — do not rebuild a second executive console.

## Landing

| Item | Value |
|------|--------|
| PackId | `hospital-admin` |
| Persona | `ExecutiveAdvisor` |
| Default home | `/app/dashboard?persona=hospital-admin` |
| Roles | ORG_ADMIN, GROUP_ADMIN, SUPER_ADMIN (+ HospitalOwner / HospitalAdmin aliases) |

Registry: `ComplianceService/Application/RoleExperience/RoleExperiencePackRegistry.cs`  
WebApp fallback: `WebApp/src/app/core/services/role-experience.service.ts`

## Executive workspace composition

Surfaced on the hospital-admin dashboard persona:

| Signal | Source |
|--------|--------|
| Readiness % | Dashboard summary / snapshot completion readiness |
| Compliance score | Snapshot overall score + gauges |
| Executive KPIs | `home-executive-kpi-strip` + Owner attention KPI strip |
| Dept performance | `home-department-heatmap` + avg dept score KPI |
| Top risks | Owner risk list (weak depts + CAPAs + approvals) |
| Critical CAPAs | Role-experience `attentionCounts.criticalCapas` |
| Upcoming audits | Role-experience `attentionCounts.upcomingAudits` |
| Trends | Compliance gauge row (`scoreTrends`) / analytics hub |
| Priorities (NBA) | `GET /role-experience/home` → `app-priority-nba-strip` |

### Widget allow-list (Owner)

`home-executive-kpi-strip`, `home-alert-banner`, `home-compliance-gauge`, `home-hospital-overview`, `home-framework-overview`, `home-department-heatmap`, `home-risk-distribution`, `home-milestones`, `home-score-trend`, plus CC tags for control-center reuse.

### Quick actions

| Label | Route |
|-------|--------|
| Generate report | `/app/reports/readiness` |
| Review risks | `/app/ccc/risk-escalation` |
| Audit calendar | `/app/audit/calendar` |
| Approvals | `/app/governance/approvals` |
| Department performance | `/app/analytics/departments` |
| Maturity | `/app/compliance-maturity` |

### Drill-downs

- **Dept Performance KPI** → `/app/analytics/departments`
- Heatmap header links → departments analytics + `/app/compliance-maturity`
- Readiness card → readiness reports
- Critical CAPAs → DCC `/app/audit/corrective-actions`

## Persona Intelligence (Owner rules)

Sprint 2 rules under `Application/Copilot/Rules/RoleExperience/`:

| Rule | Intent | Deep link |
|------|--------|-----------|
| `OwnerExecutiveAttentionRule` | Critical exposure + overdue CAPAs (Sprint 1) | Control center |
| `OwnerTopBusinessRisksRule` | Top business risks | Risk escalation |
| `OwnerDecliningDepartmentsRule` | Declining / at-risk departments | Analytics departments |
| `OwnerNabHReadinessTodayRule` | NABH readiness today | Readiness reports |
| `OwnerApprovalsPendingRule` | Decisions needing approval | Governance approvals |

Also inherits broader ExecutiveAdvisor rules (`ReadinessDeclineRule`, `DepartmentRiskRule`, etc.).

## Attention counts

Best-effort from analytics in `RoleExperienceService.LoadAttentionCountsAsync`:

- overdue tasks, pending reviews, critical CAPAs (overdue CAPAs), upcoming audits (open sessions)

## Exit criteria

Owner lands on a focused executive home answering “what needs leadership attention?” with KPIs, risks, CAPAs, audits, trends, and one-click report/risk/approval/calendar actions — without hunting the kitchen-sink menu.
