# Phase 3 — Quality Manager Experience (Sprint 3)

**Sprint:** 3  
**Status:** Delivered  
**Principle:** Prefer deep-linking existing Tasks / DCC / Evidence review / Audit planning / Control Center — compose an operational home, do not duplicate CRUD.

## Landing

| Item | Value |
|------|--------|
| PackId | `quality-manager` |
| Persona | `QualityManagerAssistant` |
| Default home | `/app/role-homes/qm` |
| Alternate surfaces | `/app/control-center`, `/app/dashboard?persona=quality-manager` |
| Roles | MANAGER (+ QualityManager / Compliance Officer aliases) |

Registry: `ComplianceService/Application/RoleExperience/RoleExperiencePackRegistry.cs`  
UI: `WebApp/src/app/features/role-homes/qm-home.component.ts`  
Route: `/app/role-homes/qm` (`ROLE_HOMES_ROUTES`)

## Operational home composition

`QmHomeComponent` composes:

| Section | Behavior |
|---------|----------|
| NBA strip | Priorities from `GET /role-experience/home` |
| Attention KPIs | Overdue tasks, pending reviews, critical CAPAs, upcoming audits |
| Quick actions | Assign work, Review evidence, Launch audit, Generate CAPA, View trends, Control center |
| Ops cards | Deep links to Tasks, Evidence review queue, DCC CAPAs, Audit planning, Heatmap (QM dashboard), Analytics |
| Attention list | Risk list derived from attention counts |

### Quick actions

| Label | Route |
|-------|--------|
| Assign work | `/app/tasks` |
| Review evidence | `/app/evidence/review-queue` |
| Launch audit | `/app/audit/planning` |
| Generate CAPA | `/app/audit/corrective-actions` |
| View trends | `/app/analytics` |
| Control center | `/app/control-center` |

### Dashboard persona (widgets)

When QM opens `/app/dashboard?persona=quality-manager`, allow-list includes heatmap, risk distribution, milestones, framework overview, task-type breakdown, and CC action/smart-alert tags.

## Persona Intelligence (QM rules)

| Rule | Intent | Deep link |
|------|--------|-----------|
| `QualityManagerDailyPrioritiesRule` | Review + deficiency queue (Sprint 1) | DCC |
| `QualityManagerUrgentCapasRule` | Urgent / overdue CAPAs | DCC |
| `QualityManagerLaggingDepartmentsRule` | Lagging departments | Analytics departments |
| `QualityManagerOverdueRiskThisWeekRule` | Becoming overdue this week (SLA at-risk + rejected evidence) | Tasks overdue |

## Competing homes

Control Center and DCC remain available. Pack **default landing** is the composed QM role home; header actions deep-link to Control Center and the QM dashboard widgets view.

## Exit criteria

QM lands on an operational home that answers “what to do first?” with today’s queues and one-click Assign / Review / Launch audit / Generate CAPA / Trends — without rebuilding task or CAPA UIs.
