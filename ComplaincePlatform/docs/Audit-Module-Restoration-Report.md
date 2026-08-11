# Audit Module Restoration Report

**Date:** 2026-07-29  
**Scope:** WebApp Audit / Auditor navigation + stubs + lifecycle capabilities  
**Principle:** ComplianceService SSOT (`ScheduledAudit` / `AuditSession` / `Finding` / DCC) — no new audit microservice

---

## 1. Previously existing Audit pages (inventory)

See plan Phase 1–3. Summary: pages were **not deleted from git**; sidebar discoverability collapsed after NAV-1. Header mega-menu retained routes.

---

## 2. Restored / re-surfaced pages (discoverability)

All of the following are now primary **left-sidebar** children under **Audit** (`sidebar.qm.audit` + `parentId`):

| Page | Route |
|------|-------|
| Overview | `/app/audit` |
| Planning & Calendar | `/app/audit/planning` |
| Auditor Home | `/app/auditor/dashboard` (role-gated) |
| Sessions | `/app/auditor/sessions` |
| Evidence Review | `/app/auditor/evidence-review` |
| **Assessment Workspace** | `/app/audit/assessment` (Chapter→Standard→OE tree; OE activity + evidences) |
| Observations | `/app/auditor/observations` |
| Findings | `/app/audit/findings` |
| Deficiencies / CAPA | `/app/audit/corrective-actions` |
| Simulation | `/app/audit/simulation` |
| Reports | `/app/audit/reports` |
| History | `/app/auditor/history` |
| Surveillance | `/app/audit/surveillance` |

Header Audit mega-menu remains as **optional quick-access** (same routes). Action Center Findings/CAPA shortcuts retained.

---

## 3. Modernized pages

| Page | Change |
|------|--------|
| Finding Detail `/app/audit/findings/:id` | Rebuilt on Findings Workbench detail panel (lifecycle, promotion, comments, attachments, CAPA deep-link) |
| Planning `/app/audit/planning` | List + month calendar + annual plan rollup + prep templates + recurrence + **Start session** handoff |
| Audit Dashboard | Upcoming/delayed schedule KPIs + lifecycle quick links |
| Session Workspace | Close session + close-gate error messaging; evidence library + finding detail links |
| Final Audit Reports | Links to analytics + dashboard + CAPA |
| Surveillance | New MVP page filtering Surveillance-typed schedule/sessions |

---

## 4. Permanently removed / not restored

| Item | Justification |
|------|----------------|
| Separate AuditProgram aggregate / microservice | Domain SSOT uses `ScheduledAudit` program metadata ([phase-2 audit management](commercial-readiness/phase-2/02-audit-management.md)) |
| Legacy orphan findings table components | Superseded by Findings Workbench |
| Orphan `audit.component.ts` stub | Deleted — unused / unrouted |
| Duplicate OE checklist inside Audit | Remains in Journey / Evidence; deep-linked from planning/session |
| Continuous Surveillance product (beyond typed audits) | Deferred; MVP is Surveillance-typed schedule/sessions only |

---

## 5. Navigation map

```
Quality Management
└── Audit (expandable)
    ├── Overview
    ├── Planning & Calendar
    ├── Auditor Home *
    ├── Sessions *
    ├── Evidence Review *
    ├── Observations *
    ├── Findings
    ├── Deficiencies / CAPA
    ├── Simulation
    ├── Reports
    ├── History *
    └── Surveillance

Action Center (shortcuts)
├── Findings → /app/audit/findings
└── Deficiencies / CAPA → /app/audit/corrective-actions

Header → Audit mega-menu (secondary quick-access)
* role-gated for auditor/admin roles
```

---

## 6. Workflow coverage

```
Annual Plan (planning → Annual plan tab)
    ↓
Schedule / Calendar (list + calendar + recurrence)
    ↓
Preparation (prep templates + team notes)
    ↓
Execution (Start session → /app/auditor/sessions/:id)
    ↓
Evidence (Evidence Review + Evidence library links)
    ↓
Observations (session + /app/auditor/observations)
    ↓
Findings (workbench + full detail + promote)
    ↓
CAPA (DCC / corrective-actions)
    ↓
Verification / Closure (session close gate UX)
    ↓
Reporting (audit reports + /app/reports/audit)
    ↓
Analytics (dashboard KPIs + chapter heatmap)
```

---

## Key files touched

- `WebApp/src/app/core/navigation/registry/menu.registry.ts`
- `WebApp/src/app/core/navigation/sidebar/sidebar.component.ts`
- `WebApp/src/app/features/audit/routes.ts`
- `WebApp/src/app/features/audit/pages/*` (detail, planning, surveillance, dashboard, reports)
- `WebApp/src/app/features/audit/services/audit-prep-template.service.ts`
- `WebApp/src/app/features/auditor/pages/auditor-session-workspace-page.component.ts`
- `WebApp/src/app/features/auditor/models/auditor-workspace.model.ts` (`scheduledAuditId`)
