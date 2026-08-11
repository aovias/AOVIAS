# Shell sidebar — navigation model (Phase 46)

**Policy:** No modules removed; routes are **regrouped** into named sections. **Journey** is the **hero** entry for compliance execution (strongest visual emphasis; default landing remains product-owned—typically `/app/dashboard` or `/app/journey`).

## Section order (top → bottom)

1. **Brand & system** (in-shell strip: product name + collapse control; global logo remains in header).
2. **Context panel** (elevated card): organization, hospital, framework, **operational mode**, optional compliance progress; **change context** opens existing hospital/framework switcher; **mode** changed via sidebar control (session-backed).
3. **Quick actions** (icon strip): Add Task, Upload Evidence, Create Finding — routes to existing flows.
4. **Dashboard** — Overview, Analytics & Insights.
5. **Compliance Execution** — Journey (primary), Standards & Chapters, Evidence (with sub-routes), Tasks.
6. **Action Center** — Findings, Issues & Risks, Alerts (badges: neutral / warning / critical).
7. **Reports** — existing reports hub + children (preserved).
8. **Audit Management** — Internal Audit, Final Audit, Surveillance (stub/disabled as documented).
9. **Knowledge & Documentation** — Policies (stub), Documents, Knowledge Graph.
10. **Administration** — Compliance Control Center (existing CCC children).

## Route mapping (representative)

| Item | Route |
|------|--------|
| Overview | `/app/dashboard` |
| Analytics & Insights | `/app/control-center/compliance` |
| Journey | `/app/journey` |
| Standards & Chapters | `/app/journey/execution` |
| Evidence / Binder / Upload | `/app/evidence`, `/app/evidence/binder`, `/app/evidence` (upload TBD) |
| Tasks | `/app/tasks` |
| Findings | `/app/audit/findings` |
| Issues & Risks | `/app/audit/corrective-actions` |
| Alerts | `/app/control-center/tasks` |
| Reports hub | `/app/reports` (+ children unchanged) |
| Internal Audit | `/app/audit` |
| Final Audit | `/app/audit/reports` |
| Surveillance | disabled / coming soon |
| Documents | `/app/evidence/binder` |
| Knowledge Graph | `/app/control-center/knowledge-graph` |
| CCC children | `/app/ccc/*` (unchanged) |

## Badge rule

- Hide badge when count is **0** (reduces noise). Cap display at **99+**.

## Extension (50+ modules)

Add rows via **sidebar-nav.model.ts** (or future registry): `sectionId`, `sortKey`, optional `featureFlag`, optional `badgeSource`.
