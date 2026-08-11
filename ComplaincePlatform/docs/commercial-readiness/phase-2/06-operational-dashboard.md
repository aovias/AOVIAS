# Phase 2 — Operational Dashboards (Sprint 6)

**Sprint:** 6  
**Status:** Delivered (persona + nav ops homes)  
**Principle:** Extend existing dashboard / Control Center / DCC / Tasks — do **not** invent a parallel ops shell.

## Role operational homes

| Persona (UI) | Default roles | Emphasis |
|--------------|---------------|----------|
| Quality Manager | `MANAGER` | KPIs, alerts, department heatmap, risk, milestones, framework |
| Department Head | `MANAGER` (manual tab) | Alerts, department heatmap, risk, milestones |
| Hospital Admin | `ORG_ADMIN` / `GROUP_ADMIN` / `SUPER_ADMIN` | Executive KPIs, hospital overview, framework |
| Consultant | `EXTERNAL_AUDITOR` / `REGULATORY_ADMIN` / `COMPLIANCE_VIEWER` | Readiness-oriented: KPIs, alerts, gauge, framework, milestones, risk |

WebApp: `features/dashboard/pages/dashboard.component.ts` — persona tabs + `PERSONA_WIDGETS`.

## Preferred ops homes (nav)

Minimal sidebar clarifications in `sidebar-nav.model.ts`:

| Home | Route | Purpose |
|------|-------|---------|
| Command Center | `/app/control-center` | Live compliance operations |
| Issues & Risks (DCC) | `/app/audit/corrective-actions` | CAPA / RCA / deficiency workbench |
| Tasks | `/app/tasks` | Execution queue |
| Analytics Hub → Operational | `/app/analytics/operational` | Aging, SLA, overdue, audit completion |

## Operational search (header)

WebApp `app-global-ops-search` in the shell header calls Gateway `GET /api/v1/compliance/search`. Graph search now includes tasks, audit sessions, and deficiency CAPA numbers. Saved queries persist in `localStorage` (`praman.ops.savedSearches`).

## Exit criteria

- Four persona layouts selectable on the shared dashboard  
- Nav points Quality / Ops users to Control Center + DCC + Tasks without a new product shell  
- Consultant defaults for advisory / external auditor style roles  
- Global ops search available from the authenticated shell  
