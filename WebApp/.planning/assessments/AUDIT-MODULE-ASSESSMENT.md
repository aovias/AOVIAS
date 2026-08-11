# Enterprise Audit Module — Capability Assessment & Evolution Strategy

**Scope:** `src/app/features/audit`, `src/app/features/auditor`, `src/app/features/admin-audit`, plus the
backend surfaces they consume (`ComplianceService`, `Gateway`, `AuditService`).
**Method:** static inventory of routes, components, services, models, backend controllers, and the
navigation registry. No runtime/authenticated walkthrough was performed — see *Assessment Confidence*.
**Nature:** assessment only. No code was changed.

---

## Executive Summary

The Audit module is **substantially more mature than it appears from the navigation**. It is
genuinely API-backed end to end — there is **no mock or placeholder data anywhere** in the 15 audit
pages (every "mock" hit in the source is an input `placeholder` attribute). Two of its sub-modules,
the **Findings Workspace** and the **Deficiency Command Center**, are deep, well-modelled enterprise
capabilities that would take months to rebuild.

The real weaknesses are not in the domain modelling — they are in **lifecycle completeness,
persistence, governance, and consolidation**:

| Theme | Finding |
|---|---|
| **Strongest asset** | CAPA domain: RCA → corrective → preventive → effectiveness review, with SLA, escalation history, and auto-assignment. This is genuinely enterprise-grade. |
| **Biggest structural gap** | Scheduled audits are **create-only**. The backend exposes `GET`, `GET /calendar`, `POST` — and no `PUT`/`PATCH`/`DELETE`. You cannot edit, reschedule, or cancel an audit once created. |
| **Biggest data-integrity gap** | Audit prep **templates live in `localStorage`**. They are per-browser, per-device, and invisible to colleagues. |
| **Biggest governance gap** | `/app/audit/**` has **no route guards at all**. Only `/app/auditor/**` is role-protected. |
| **Wasted investment** | A dedicated `AuditService` microservice exposing `POST /api/v1/audit/generate` is proxied through the Gateway and has **zero UI consumers**. |
| **Dead code** | `FindingApiService` (no consumers) and the `audit/components/audit-findings-*` trio (an orphaned island superseded by Findings Workspace). |
| **Fragmentation** | Audit capability is split across three top-level features (`audit`, `auditor`, `admin-audit`) with cross-imports in both directions and three separate "reports" surfaces. |

**Recommended posture: enhance, don't rebuild.** The domain model is the expensive part and it is
already good. The roadmap below is weighted toward completing lifecycles and consolidating
navigation, not redesigning screens.

---

## Assessment Confidence

Stated plainly, because it affects how much weight to put on each section:

| Section | Confidence | Basis |
|---|---|---|
| Capability inventory, routes, services, API surface | **High** | Direct source and controller inspection |
| Dead code, orphaned services, missing HTTP verbs | **High** | Exhaustive grep for consumers; controller attribute enumeration |
| Workflow gaps | **High** | Derived from missing endpoints + missing routes |
| Screen assessment — purpose, structure, reuse | **High** | Template and import inspection |
| Screen assessment — **UX quality in use** | **Low–Medium** | The app requires authentication and I did not sign in. UX judgements are inferred from markup, density, and component structure, **not from seeing the screens run**. Treat these as hypotheses to confirm. |
| Backend behaviour beyond routing | **Medium** | Controller signatures inspected; service/business logic not deeply reviewed |

---

## 1. Current Capability Inventory

### 1.1 Route map

**`/app/audit/**` — internal audit (no route guard)**

| Route | Component | LOC | Backing service |
|---|---|---|---|
| `/app/audit` | `AuditDashboardComponent` | 251 | AuditManagementApi, AuditorApi, ControlCenter, ReportsApi |
| `/app/audit/planning` | `AuditPlanningComponent` | 536 | AuditManagementApi, AuditPrepTemplate, AuditorApi |
| `/app/audit/findings` | `AuditFindingsPageComponent` | 527 | FindingsWorkspaceService |
| `/app/audit/findings/:id` | `AuditFindingDetailComponent` | 249 | FindingsWorkspaceService |
| `/app/audit/corrective-actions` | `CorrectiveActionsComponent` | 641 | DeficiencyCommandCenter, DeficiencyData, ComplianceMaturity, MembershipScope |
| `/app/audit/simulation` | `AuditSimulationComponent` | 101 | AuditManagementApi |
| `/app/audit/reports` | `AuditReportsComponent` | 151 | AuditorApi, ReportsApi |
| `/app/audit/surveillance` | `AuditSurveillanceComponent` | 183 | AuditManagementApi, AuditorApi |
| `/app/audit/assessment` | `AuditorAssessmentPageComponent` *(borrowed from `auditor`)* | 369 | AuditorApi |
| `/app/audit/internal` | → redirect to `/app/audit` | — | — |
| `/app/audit/final` | → redirect to `/app/audit/reports` | — | — |
| `/app/audit/calendar` | → redirect to `/app/audit/planning` | — | — |

**`/app/auditor/**` — external auditor / regulator (guarded: `EXTERNAL_AUDITOR`, `REGULATOR`, `REGULATORY_ADMIN`, `SUPER_ADMIN`, `ORG_ADMIN`)**

| Route | Component | LOC |
|---|---|---|
| `/app/auditor/dashboard` | `AuditorDashboardPageComponent` | 177 |
| `/app/auditor/sessions` | `AuditorSessionsPageComponent` | 174 |
| `/app/auditor/sessions/:id` | `AuditorSessionWorkspacePageComponent` | 280 |
| `/app/auditor/evidence-review` | `AuditorAssessmentPageComponent` *(same component as `/app/audit/assessment`)* | 369 |
| `/app/auditor/observations` | `AuditorObservationsPageComponent` | 133 |
| `/app/auditor/findings` | `AuditFindingsPageComponent` *(borrowed from `audit`)* | — |
| `/app/auditor/reports` | `AuditorReportsPageComponent` | 115 |
| `/app/auditor/history` | `AuditorHistoryPageComponent` | 121 |

**`/app/admin-audit/**` — system activity log.** `AuditActivityPageComponent` + detail dialog.
This is an **administrative audit trail** (who did what in the system), not compliance auditing. It
shares only the word "audit". Keep it separate; it is out of scope for this suite.

### 1.2 Sub-workspaces (the module's real depth)

**Findings Workspace** — `audit/findings-workspace/`
Components: `findings-dashboard` (212), `findings-grid` (211), `findings-detail-panel` (444),
`findings-create-panel` (439). Service, models, export util.

**Deficiency Command Center (CAPA)** — `audit/deficiency-command-center/`
Components: `dcc-dashboard` (655), `dcc-grid` (415), `dcc-detail-panel` (**1,494**). Service, models,
export util. The detail panel is the single largest component in the module.

### 1.3 API surface actually consumed

| Service | Endpoints |
|---|---|
| `AuditManagementApiService` | `GET /audit-schedule`, `GET /audit-schedule/calendar`, `POST /audit-schedule`, `POST /auditor/simulation` |
| `AuditorApiService` | `GET /auditor/dashboard`, `GET /auditor/sessions`, `POST /auditor/sessions`, `GET /auditor/session/{id}`, `POST /auditor/sessions/{id}/close`, `POST /auditor/observations`, `POST /auditor/comments`, `GET /auditor/traceability/{oeId}`, `GET /auditor/report/{sessionId}`, `GET /auditor/assessment-context` |
| `FindingsWorkspaceService` | `GET /findings/workspace{,/dashboard,/compliance-context,/{id}}`, `POST /findings`, `PATCH /findings/{id}/status`, `POST /findings/{id}/comments`, `POST /findings/{id}/attachments`, `GET /findings/{id}/eligibility`, `GET /findings/{id}/promotion-status`, `POST /findings/{id}/promote`, `GET /assignment/findings/{id}/suggestion` |
| `DeficiencyCommandCenterService` | `GET /deficiencies/command-center{/dashboard,/workspace,/workspace/{id}}`, `PATCH /deficiencies/{id}/{root-cause,rca,remediation,corrective-action,preventive-action,effectiveness-review}`, `GET /deficiencies/{id}/capa`, `POST /deficiencies/{id}/{accept,reject-ownership}`, `GET|POST /assignment/deficiencies/{id}/…`, `POST /sla/deficiencies/{id}/acknowledge-escalation` |
| `AuditPrepTemplateService` | **None — `localStorage` key `praman.audit.prepTemplates.v1`** |
| `FindingApiService` | `GET /findings`, `GET /findings/{id}`, `POST /findings` — **zero consumers (dead)** |

### 1.4 Domain model depth

**Findings** — Classification (`AuditFinding`, `Observation`, `NonConformity`,
`OpportunityForImprovement`, `GoodPractice`); Source (8 values incl. `InternalAudit`,
`ExternalAudit`, `NabhAssessment`, `ConsultantAssessment`, `IncidentReview`); Severity (5);
Lifecycle (`Draft → Open → Reviewed → Accepted → Closed`, plus `InProgress`, `Verified`);
**Promotion readiness** (`Not Eligible → Awaiting Acceptance → Eligible → Promoted`) with dedicated
eligibility/status/promote endpoints; attachments, comments, history, quick filters.

**Deficiencies / CAPA** — Severity, Status, **LifecycleStatus**, RiskLevel,
**RootCauseCategory**, **EffectivenessReviewStatus**, `CapaActionStatus`, escalation history, SLA
acknowledgement, ownership accept/reject, assignment-confidence routing suggestions.

**Scheduling** — 9 `AuditType` values: `InternalAudit`, `DepartmentAudit`, `FollowUpAudit`,
`MockAudit`, `FocusedAudit`, `ConsultantReview`, `NabhPreAssessment`, `NabhFinalAssessment`,
`Surveillance`. Program year/month/name fields exist on the DTO.

### 1.5 Capability matrix

| Area | Feature | Status | Notes |
|---|---|---|---|
| Planning | Schedule an audit | **Complete** | 9 audit types, program year/month/name |
| Planning | Calendar view | **Complete** | `GET /audit-schedule/calendar`, month grid |
| Planning | Annual plan view | **Partial** | View exists; no capacity or coverage model |
| Planning | Prep templates | **Partial** | **`localStorage` only — not shared, not persisted** |
| Planning | Recurring audits | **Partial** | Client-side loop creates N independent audits; no series entity |
| Planning | Edit / reschedule / cancel | **Missing** | **No `PUT`/`PATCH`/`DELETE` on `/audit-schedule`** |
| Planning | Risk-based plan generation | **Missing** | — |
| Planning | Capacity / workload planning | **Missing** | — |
| Execution | Audit sessions | **Complete** | Create, open, close |
| Execution | Session workspace | **Complete** | Observations + comments inline |
| Execution | Assessment workspace (Chapter→Standard→OE tree) | **Complete** | Shared by internal and external routes |
| Execution | Checklist / structured protocol | **Missing** | No checklist entity or endpoint |
| Execution | Clause guidance in-context | **Missing** | — |
| Execution | Previous-audit context | **Missing** | History page exists but is not surfaced during execution |
| Execution | Reopen a closed session | **Missing** | Close is one-way |
| Evidence | Traceability by OE | **Complete** | `GET /auditor/traceability/{oeId}` |
| Evidence | Attach to finding | **Complete** | `POST /findings/{id}/attachments`, 5 attachment types |
| Evidence | Review queue / approval | **Partial** | Lives in the Evidence module, not integrated into audit flow |
| Evidence | Versioning / expiry / duplicate detection | **Missing** | — |
| Observations | Capture | **Complete** | `POST /auditor/observations` |
| Observations | Promote observation → finding | **Partial** | Finding promotion exists; observation→finding path unclear |
| Findings | Workspace (dashboard, grid, detail, create) | **Complete** | Strongest screen set in the module |
| Findings | Lifecycle transitions | **Complete** | `PATCH /findings/{id}/status` |
| Findings | Promotion to deficiency | **Complete** | Eligibility + status + promote |
| Findings | Comments | **Complete** | — |
| Findings | Assignment suggestion | **Complete** | `/assignment/findings/{id}/suggestion` |
| Findings | Bulk actions | **Missing** | — |
| Findings | Delete / withdraw | **Missing** | No `DELETE` |
| Findings | Repeat-finding detection | **Missing** | — |
| CAPA | Root cause analysis | **Complete** | Categories + free-text final root cause |
| CAPA | Corrective action | **Complete** | — |
| CAPA | Preventive action | **Complete** | — |
| CAPA | Effectiveness review | **Complete** | Status + schedule options |
| CAPA | SLA + escalation | **Complete** | History + acknowledgement |
| CAPA | Ownership accept / reject | **Complete** | — |
| CAPA | Auto-assignment | **Complete** | Confidence-scored routing |
| Reporting | Session report | **Complete** | `GET /auditor/report/{sessionId}` |
| Reporting | Final audit reports | **Complete** | `/app/audit/reports` |
| Reporting | Generated audit document | **Missing (UI)** | **`POST /api/v1/audit/generate` exists and is unused** |
| Reporting | Executive audit dashboard | **Partial** | KPI cards + findings-by-chapter + recent activity |
| Reporting | Trends / heat maps / repeat findings | **Missing** | — |
| Simulation | Mock audit scoring | **Complete** | NC counts, score, high-risk chapters |
| Surveillance | Surveillance view | **Complete** | — |
| Admin | Audit types / templates config | **Missing** | Types are a hard-coded union |
| Admin | Team management / resource planning | **Missing** | Lead auditor + team notes are free text |
| Governance | Route guards on internal audit | **Missing** | **`/app/audit/**` unguarded** |
| Collaboration | Comments | **Complete** | Findings + sessions |
| Collaboration | Mentions / discussions / notifications | **Missing** | — |

---

## 2. Workflow Coverage Map

```
Audit Creation   ✅  /app/audit/planning — create panel, 9 types
      ↓
Planning         🟡  Annual plan + calendar views exist; no capacity/risk basis
      ↓          ❌  BREAK: cannot edit, reschedule, or cancel after creation
Scheduling       🟡  Recurrence is a client-side loop → N orphan audits, no series
      ↓
Preparation      🟡  Prep templates exist but are localStorage-only
      ↓          ❌  BREAK: no checklist / audit protocol entity
Execution        ✅  Sessions + session workspace + assessment tree
      ↓          ❌  BREAK: close is one-way, no reopen
Evidence         🟡  Traceability + attachments strong; review/approval lives outside audit
      ↓
Observation      ✅  Capture with comments
      ↓          🟡  BREAK: observation → finding promotion path not explicit
Finding          ✅  Full workspace + lifecycle + promotion readiness
      ↓
CAPA             ✅  RCA → corrective → preventive → effectiveness → SLA/escalation
      ↓
Closure          🟡  Session close + finding Closed exist; no audit-level closure with
                     sign-off, report attachment, or management review
```

### Specific defects

| Type | Detail |
|---|---|
| **Missing transition** | Scheduled audit → session. Planning navigates to `/app/auditor/sessions/:id` after create, but there is no persisted link between a `ScheduledAudit` and an `AuditSession`. |
| **Missing action** | Edit/cancel a scheduled audit; reopen a closed session; withdraw a finding; delete a template. |
| **Broken navigation** | `/app/auditor/findings` and `/app/audit/findings` render the **same component** with different breadcrumb context. `/app/audit/assessment` and `/app/auditor/evidence-review` are likewise the same component on two routes. |
| **Duplicate functionality** | Three reporting surfaces: `/app/audit/reports`, `/app/auditor/reports`, and the separate `reporting` feature. |
| **Duplicate functionality** | `audit/components/audit-findings-{filters,row,table}` duplicate the Findings Workspace grid and are unreferenced. |
| **Incomplete workflow** | Recurrence produces independent rows — editing "the series" is impossible by construction. |

---

## 3. Screen-by-Screen Assessment

UX quality below is **inferred from source**, not observed running (see *Assessment Confidence*).

| Screen | Purpose | Primary user | Business value | Maturity | Verdict |
|---|---|---|---|---|---|
| Audit Dashboard | Program KPIs, findings by chapter, activity | Quality Manager | High | Medium — 4 services, 3 widgets | **Enhance** — add trends, repeat findings, readiness |
| Planning & Calendar | Schedule, calendar, annual plan, templates | Audit Lead | **Very High** | Medium — 4 views in one 536-line page | **Enhance** — needs edit/cancel + server templates |
| Findings Page | Findings workspace | QM / Auditor | **Very High** | **High** | **Preserve** |
| Finding Detail | Single finding + lifecycle | QM | High | High | **Preserve** |
| Corrective Actions (CAPA) | Deficiency command center | QM / Owner | **Very High** | **High** — richest in module | **Preserve** |
| Simulation | Mock audit scoring | QM | Medium | Low — 101 LOC, read-only | **Enhance** — no history or comparison |
| Audit Reports | Final findings rollup + export | QM / Leadership | High | Low–Medium — 151 LOC | **Consolidate** with auditor reports |
| Surveillance | Surveillance audits | QM | Medium | Low–Medium — 183 LOC | **Merge** into Planning as an audit-type filter |
| Assessment Workspace | Chapter→Standard→OE tree, timeline, evidence | Auditor | **Very High** | High — 369 LOC | **Preserve** |
| Auditor Dashboard | External auditor home | External Auditor | High | Medium | **Enhance** |
| Auditor Sessions | Session list + create | External Auditor | High | Medium | **Enhance** — no edit/cancel |
| Session Workspace | Live execution: observations + comments | External Auditor | **Very High** | Medium — 280 LOC | **Enhance** — the natural home for a checklist |
| Observations | Observation list | External Auditor | Medium | Low — 133 LOC | **Enhance** — add promote-to-finding |
| Auditor Reports | Session reports | External Auditor | Medium | Low — 115 LOC | **Consolidate** |
| Auditor History | Past sessions | External Auditor | Medium | Low — 121 LOC | **Enhance** — feed into execution context |
| `audit-findings-{filters,row,table}` | Superseded findings UI | — | **None** | Dead | **Remove** |
| `FindingApiService` | Superseded findings API | — | **None** | Dead | **Remove** |
| Admin Audit Activity | System activity log | Admin | High | Medium | **Keep separate** — different domain |

---

## 4. Functional Domain Mapping

| Domain | Existing assets |
|---|---|
| **Audit Planning** | `audit-planning` (schedule/calendar/annual/templates), `AuditManagementApiService`, `AuditPrepTemplateService`, `audit-surveillance` |
| **Audit Execution** | `auditor-sessions`, `auditor-session-workspace`, `auditor-assessment-page`, `AuditorApiService` |
| **Evidence** | `GET /auditor/traceability/{oeId}`, `POST /findings/{id}/attachments`; external Evidence module |
| **Findings** | `findings-workspace/**`, `audit-findings-page`, `audit-finding-detail`, `FindingsWorkspaceService` |
| **CAPA** | `deficiency-command-center/**`, `corrective-actions`, `DeficiencyCommandCenterService` |
| **Reporting** | `audit-reports`, `auditor-reports`, `audit-dashboard`, `audit-simulation`, unused `AuditService.generate` |
| **Administration** | *Effectively empty* — audit types hard-coded, no team management, no config screens |

**Observation:** six of seven domains have real assets. **Administration is a genuine void**, and it
is what currently forces templates into `localStorage` and auditors into free-text fields.

---

## 5. Gap Analysis

Priority: **P1** blocks core audit work · **P2** enterprise expectation · **P3** differentiator.

### Planning
| Gap | Priority | Note |
|---|---|---|
| Edit / reschedule / cancel scheduled audit | **P1** | Needs backend verbs first |
| Server-persisted prep templates | **P1** | Replace `localStorage` |
| True recurring-audit series | **P2** | Series entity + regenerate/skip |
| Annual plan with coverage tracking | **P2** | Plan vs actual |
| Risk-based planning | **P3** | Feed from Risk module |
| Capacity / workload planning | **P3** | Requires team management |

### Execution
| Gap | Priority |
|---|---|
| Checklist / audit protocol | **P1** |
| Reopen closed session | **P2** |
| Split-pane live workspace (tree ‖ capture) | **P2** |
| Clause guidance in context | **P2** |
| Previous-audit context during execution | **P3** |

### Evidence
| Gap | Priority |
|---|---|
| Evidence review/approval inside audit flow | **P1** |
| Version history | **P2** |
| Expiry tracking | **P2** |
| Quality scoring | **P3** |
| Duplicate detection | **P3** |

### Collaboration
| Gap | Priority |
|---|---|
| Notifications on assignment / SLA breach | **P1** |
| Task creation from finding | **P2** |
| Mentions | **P2** |
| Threaded discussions | **P3** |

### Intelligence
| Gap | Priority |
|---|---|
| Repeat-finding detection | **P1** — a core audit concept, currently absent |
| Trend analysis | **P2** |
| Audit readiness score | **P2** — simulation gives a point-in-time score only |
| Heat maps | **P3** |

### Specialized audit types
Types exist as an enum but **no type-specific behaviour**: all 9 produce an identical flow. Mock
audit has simulation; surprise audit, re-audit, and external surveillance have no distinct handling.
**P2.**

### Enterprise operations
| Gap | Priority |
|---|---|
| **Route guards on `/app/audit/**`** | **P1 — governance defect** |
| Team management | **P2** |
| Approval workflow (plan/report sign-off) | **P2** |
| Calendar integration (iCal/Outlook) | **P3** |
| Workload distribution | **P3** |

---

## 6. Architecture Assessment

### Strengths
- Standalone components with `ChangeDetectionStrategy.OnPush` and signal-based state throughout.
- Services extend a shared `BaseApiService`; consistent context injection via `ComplianceContextService`.
- Sub-workspaces (`findings-workspace`, `deficiency-command-center`) use a clean
  **models / services / components / utils** internal structure — this is the right pattern and is
  worth generalising.
- Lazy-loaded routes throughout.

### Weaknesses
1. **Three-way feature split with bidirectional imports.** `audit/routes.ts` imports a page from
   `auditor/`; `auditor/routes.ts` imports a page from `audit/`. This is a dependency cycle at the
   feature level and will make either module hard to extract or test in isolation.
2. **Page components carry too much.** `dcc-detail-panel` at 1,494 LOC and `corrective-actions` at
   641 LOC mix orchestration, form state, and presentation.
3. **Inconsistent internal structure.** `audit/pages/*` are flat; the two sub-workspaces are
   well-structured. The flat pages should adopt the sub-workspace pattern.
4. **No shared audit domain layer.** Finding, deficiency, and session models are defined in three
   places with overlapping severity/status vocabularies (`FindingSeverityWire` vs `FindingSeverity`
   vs `DeficiencySeverity`).
5. **Client-side business logic.** Recurrence expansion lives in the component
   (`buildRecurrenceDates()`), so the rule cannot be enforced or changed server-side.
6. **Dead code retained.** `FindingApiService`, `audit-findings-{filters,row,table}`, and the legacy
   halves of `finding.models.ts` (only `toFindingWireEnum` is still used).

### Recommended refactoring — only where it enables growth
| Change | Rationale | Effort |
|---|---|---|
| Delete dead code | Removes ambiguity about which findings stack is current | XS |
| Extract `audit/shared/` domain layer (severity, status, audit-type vocabularies) | Stops the three-way divergence | S |
| Promote flat `audit/pages/*` to the sub-workspace pattern | Consistency; enables testing | M |
| Resolve the `audit` ↔ `auditor` cycle via a shared feature or explicit re-export | Removes the cycle | M |
| Split `dcc-detail-panel` into tab-level child components | 1,494 LOC is unmaintainable | M |

**Explicitly not recommended:** rewriting the Findings Workspace or Deficiency Command Center. They
are the module's most valuable assets.

---

## 7. UX Assessment

*Inferred from source. Confirm against the running app before acting on the Medium/Low-confidence items.*

| Dimension | Assessment |
|---|---|
| **Navigation clarity** | **Weak.** 12 sidebar children under "Audit" with no grouping, spanning two role models. "Internal Audit" points at the dashboard; "Final Audit Reports", "Sessions", "History", and "Observations" are all siblings. |
| **Duplicate entry points** | Same component reachable from two routes in two places (findings, assessment). Users cannot tell whether they are in the same screen. |
| **Information density** | Likely high on CAPA (19-column grid) and Findings. Both now use the standard `.aov-table` pattern after the recent UI sprint. |
| **Layout consistency** | **Good, recently.** All audit pages now use the shared 25px page container and the standardized table/KPI patterns. |
| **Discoverability** | Weak. Simulation, Surveillance, and Assessment Workspace are buried; templates are invisible to anyone but their author. |
| **Workflow continuity** | **The main friction.** Planning → session, observation → finding, and finding → CAPA are conceptually connected but require manual re-navigation. |
| **Context switching** | High. Executing an audit means moving between session workspace, assessment tree, evidence, and findings as separate pages. |
| **Accessibility** | Improved by the recent sprint (focus rings, `scope="col"`, `aria-selected`, touch targets). Sortable headers on these grids emit sort events but **do not set `aria-sort`**, so the state is invisible to screen readers. |
| **Responsive** | Wide grids scroll inside their wrappers; 19-column CAPA grid will be difficult below laptop width. |
| **Theming** | Good — both themes supported after the token migration. |

### Top friction points
1. **No single "audit in progress" home.** Executing an audit spans 4+ screens with no persistent context.
2. **12 flat nav items** conflate internal and external auditor journeys.
3. **Templates invisible to the team** — the feature appears broken to a second user.
4. **Dead-end actions** — no edit or cancel on a scheduled audit, no reopen on a closed session.

---

## 8. Strategic Recommendations

### Preserve (UI modernization only)
- Findings Workspace (dashboard, grid, detail, create)
- Deficiency Command Center / CAPA — RCA, corrective, preventive, effectiveness, SLA, escalation
- Assessment Workspace (Chapter→Standard→OE tree with traceability)
- Finding promotion model (eligibility → status → promote)
- Audit type taxonomy (9 types) and the scheduling DTO
- Assignment-suggestion integration

### Enhance
- **Planning** → add edit/cancel, server templates, real recurrence series
- **Session Workspace** → checklist, split-pane, previous-audit context, reopen
- **Audit Dashboard** → trends, repeat findings, readiness score
- **Observations** → explicit promote-to-finding
- **Simulation** → run history and comparison
- **Findings** → bulk actions, withdraw, repeat detection

### Consolidate
- Three reporting surfaces → one Audit Reporting hub
- `/app/audit/findings` + `/app/auditor/findings` → one route, role-aware
- `/app/audit/assessment` + `/app/auditor/evidence-review` → one route
- Surveillance → an audit-type view within Planning
- Severity/status vocabularies → one shared domain layer

### Introduce
- **Audit Administration** (types, templates, teams, checklists) — closes the empty domain
- **Route guards on `/app/audit/**`** — governance defect
- **Surface `AuditService.generate`** — existing, unused investment
- Checklist / audit protocol entity
- Evidence review integrated into the audit flow
- Notifications and task creation from findings
- Repeat-finding detection
- Approval workflow for plans and reports

---

## 9. Prioritized Improvement Backlog

**Wave 1 — Correctness & governance** *(unblocks everything; small)*
1. Add role guards to `/app/audit/**` — **security/governance defect**
2. Delete dead code (`FindingApiService`, `audit-findings-*` trio)
3. Add `aria-sort` to sortable grid headers
4. Add `PUT`/`PATCH`/`DELETE` to `/audit-schedule` **(backend)** and wire edit/cancel into Planning

**Wave 2 — Close the lifecycle** *(highest user-visible value)*
5. Move prep templates to a server API
6. Recurring-audit series entity (create/skip/regenerate)
7. Link `ScheduledAudit` → `AuditSession` and navigate the handoff
8. Reopen a closed session
9. Explicit observation → finding promotion

**Wave 3 — Consolidation**
10. Merge the three reporting surfaces
11. Deduplicate the shared findings and assessment routes
12. Restructure the Audit sidebar into Plan / Execute / Findings / CAPA / Reports
13. Extract `audit/shared/` domain vocabulary

**Wave 4 — Enterprise capability**
14. Audit Administration module (types, templates, teams, checklists)
15. Checklist-driven execution in the session workspace
16. Evidence review inside the audit flow
17. Surface `AuditService.generate` for audit report documents
18. Notifications + task creation from findings

**Wave 5 — Intelligence**
19. Repeat-finding detection
20. Trend analysis and audit heat maps
21. Audit readiness score
22. Risk-based planning fed by the Risk module

---

## 10. Proposed Enterprise Audit Workspace Information Architecture

Reduces 12 flat items to **5 grouped destinations**, aligned to the workflow rather than to role.

```
Audit
├── Overview                     /app/audit
│     Program KPIs · trends · repeat findings · readiness · upcoming
│
├── Plan                         /app/audit/plan
│     ├── Calendar               (view)      ← today's Planning calendar
│     ├── Annual Plan            (view)      ← + coverage tracking
│     ├── Schedule               (view)      ← + EDIT / CANCEL  [new]
│     └── Templates              (view)      ← server-persisted [changed]
│         Surveillance folds in here as an audit-type filter
│
├── Execute                      /app/audit/execute
│     ├── Active Audits          (list)      ← merged sessions
│     ├── Audit Workspace        /:id        ← split-pane [enhanced]
│     │     Left: Chapter→Standard→OE tree (Assessment Workspace)
│     │     Right: checklist [new] · observations · evidence · comments
│     ├── Observations           (view)      ← + promote to finding [new]
│     └── History                (view)      ← + surfaced in-context [new]
│
├── Findings & CAPA              /app/audit/findings
│     ├── Findings               ← Findings Workspace (preserved)
│     ├── Finding Detail         /:id
│     └── CAPA                   ← Deficiency Command Center (preserved)
│
├── Reports                      /app/audit/reports
│     ├── Audit Reports          ← consolidated from 3 surfaces
│     ├── Session Reports
│     ├── Generated Documents    ← AuditService.generate [new]
│     └── Simulation             ← + run history [enhanced]
│
└── Administration               /app/audit/admin          [new domain]
      ├── Audit Types
      ├── Checklist Templates
      ├── Prep Templates
      └── Audit Teams
```

**Role handling:** external auditors see the same IA, scoped by guard — not a parallel `/app/auditor`
tree. This removes the duplicated routes and the `audit` ↔ `auditor` dependency cycle.

**Framework neutrality:** nothing above is NABH-specific. The Chapter→Standard→OE hierarchy and the
`frameworkId` already present on every DTO carry over to other frameworks; the only NABH-coupled
items are two `AuditType` values (`NabhPreAssessment`, `NabhFinalAssessment`), which the proposed
Audit Administration module would make configurable rather than hard-coded.

---

## Closing Position

The module does not need a rebuild. It needs **lifecycle completion** (edit/cancel/reopen),
**persistence** (templates), **governance** (guards), and **consolidation** (routes, reports,
vocabularies). Wave 1 is small and removes a real security gap. Waves 2–3 deliver most of the
perceived quality improvement without touching the two components that represent the module's
deepest domain investment.
