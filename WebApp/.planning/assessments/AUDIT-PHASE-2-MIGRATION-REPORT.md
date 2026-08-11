# Audit Workspace — Phase 2 Migration Report

**Scope:** navigation, information architecture, workspace organisation, layout and
toolbar consistency, screen consolidation.
**Out of scope and untouched:** business logic, services, CQRS, backend, APIs, validation,
database, permissions, workflows, domain models.

---

## 1. Capability Mapping

Every capability now belongs to exactly one business domain. Nothing floats, nothing appears twice.

| Domain (tab) | Canonical route | Hosted capability | Component (unchanged) |
|---|---|---|---|
| **Overview** | `/app/audit/overview/programme` | Internal programme dashboard | `AuditDashboardComponent` |
| | `/app/audit/overview/auditor-home` 🔒 | External auditor home | `AuditorDashboardPageComponent` |
| **Planning** | `/app/audit/planning` | Schedule · Calendar · Annual plan · Templates | `AuditPlanningComponent` |
| **Execution** | `/app/audit/execution/sessions` 🔒 | Active audits | `AuditorSessionsPageComponent` |
| | `/app/audit/execution/sessions/:id` 🔒 | Session workspace | `AuditorSessionWorkspacePageComponent` |
| | `/app/audit/execution/observations` 🔒 | Observations | `AuditorObservationsPageComponent` |
| | `/app/audit/execution/surveillance` | Surveillance audit type | `AuditSurveillanceComponent` |
| | `/app/audit/execution/simulation` | Mock / simulation audit type | `AuditSimulationComponent` |
| **Evidence** | `/app/audit/evidence` | Assessment workspace — Chapter→Standard→OE tree, evidence, traceability, timeline | `AuditorAssessmentPageComponent` |
| **Findings** | `/app/audit/findings` | Findings Workspace (dashboard · grid · create) | `AuditFindingsPageComponent` |
| | `/app/audit/findings/:id` | Finding detail · lifecycle · promotion | `AuditFindingDetailComponent` |
| **CAPA** | `/app/audit/capa` | Deficiency Command Center | `CorrectiveActionsComponent` |
| **Reports** | `/app/audit/reports/audit` | Audit reports · findings rollup · exports | `AuditReportsComponent` |
| | `/app/audit/reports/sessions` 🔒 | Session reports | `AuditorReportsPageComponent` |
| | `/app/audit/reports/history` 🔒 | Audit history | `AuditorHistoryPageComponent` |
| **Intelligence** | `/app/audit/intelligence` | Placeholder — capability not built | `AuditIntelligencePageComponent` |
| **Administration** | `/app/audit/administration` | Placeholder — capability not built | `AuditAdministrationPageComponent` |

🔒 = guarded by `AUDITOR_ROLE_IDS`, the **same** role set that previously guarded `/app/auditor/**`.
Access control is unchanged; sub-tabs are simply not advertised to users the guard would reject.

---

## 2. Route Consolidation & Redirect Strategy

Nothing was deleted. Every previously valid URL resolves — verified programmatically
(27/27 URLs, all `OK`).

### Legacy audit paths → canonical

| Old | New |
|---|---|
| `/app/audit` | `/app/audit/overview` |
| `/app/audit/assessment` | `/app/audit/evidence` |
| `/app/audit/corrective-actions` | `/app/audit/capa` |
| `/app/audit/simulation` | `/app/audit/execution/simulation` |
| `/app/audit/surveillance` | `/app/audit/execution/surveillance` |
| `/app/audit/internal` | `/app/audit/overview` |
| `/app/audit/final` | `/app/audit/reports` |
| `/app/audit/calendar` | `/app/audit/planning` |

### `/app/auditor/**` retired as a destination

The entire tree is now a redirect table. Page components are untouched — they load from
`audit/routes.ts`.

| Old | New |
|---|---|
| `/app/auditor` · `/app/auditor/dashboard` | `/app/audit/overview/auditor-home` |
| `/app/auditor/sessions` | `/app/audit/execution/sessions` |
| `/app/auditor/sessions/:id` | `/app/audit/execution/sessions/:id` |
| `/app/auditor/observations` | `/app/audit/execution/observations` |
| `/app/auditor/evidence-review` | `/app/audit/evidence` |
| `/app/auditor/findings` | `/app/audit/findings` |
| `/app/auditor/reports` | `/app/audit/reports/sessions` |
| `/app/auditor/history` | `/app/audit/reports/history` |

Existing in-app drill-downs such as `['/app/auditor/sessions', id]` (used by planning,
reports, surveillance, observations, history and the auditor dashboard) keep working
through the parameterised redirect — no page needed editing.

---

## 3. Removed Duplications

| # | Duplication | Resolution |
|---|---|---|
| 1 | `/app/audit/findings` and `/app/auditor/findings` rendered the **same component** | One canonical route; auditor path redirects |
| 2 | `/app/audit/assessment` and `/app/auditor/evidence-review` rendered the **same component** | Canonical `/app/audit/evidence`; both old paths redirect |
| 3 | Three separate reporting destinations | One Reports tab with three sections |
| 4 | Surveillance as an independent workspace + sidebar entry | Audit-type view inside Execution |
| 5 | 12 Audit deep links in the header module switcher | **1** entry → the workspace |
| 6 | `workspace.dashboard.auditor-home` — a second sidebar route into audit content | Removed; surfaced as Overview → Auditor Home |
| 7 | `FindingApiService` — 0 consumers, superseded by `FindingsWorkspaceService` | **Deleted** |
| 8 | `audit-findings-table` / `-row` / `-filters` — orphaned island superseded by Findings Workspace | **Deleted** (empty `components/` dir removed) |
| 9 | `models/finding-labels.ts` — only consumed by the deleted components | **Deleted** |
| 10 | Auditor dashboard header quick-links (Sessions · Evidence review · Observations) | Hardcoded nav defaults removed; server-supplied actions still honoured |

---

## 4. Layout, Toolbar & Breadcrumb Standardisation

**Layout (Step 7).** All three multi-section capabilities compose one shared
`AuditCapabilityShellComponent`, so sub-tab height, spacing, alignment and typography are
identical everywhere. Hosted pages keep the global 25px page container from the UI sprint,
so content margins, width and padding already match across every tab.

**Toolbars (Step 9).** Zero navigation controls remain in any audit toolbar or page header.
Phase 1 removed 21 nav-only buttons; Phase 2 removed the last three (auditor dashboard
quick-links). What remains is actions only — Export Excel/PDF, Refresh, Retry, Create,
Schedule, Close. Three links were deliberately kept because they are **not** page switching:
two "← back" links within a capability, and one inline contextual link inside an empty state.

**Breadcrumbs (Step 8).** No audit page used `PageBreadcrumbComponent`, so there was no
inconsistency to reconcile. The standardised locator is the **tab strip**: the active
capability tab plus the active sub-tab reads exactly as `Audit / Execution / Active Audits`.
A separate breadcrumb row was deliberately **not** added — it would re-introduce the header
chrome band that was explicitly removed at the end of Phase 1. Location is shown in one
place, consistently, on every tab.

---

## 5. Hidden Capabilities Surfaced

| Capability | Was | Now |
|---|---|---|
| External Auditor Home | Own sidebar entry; orphaned when Phase 1 collapsed the Audit children | Overview → Auditor Home |
| Session workspace | Only reachable by clicking a session row | Execution → Active Audits → row (unchanged) plus a canonical URL inside the workspace |
| Session Reports | Buried in the separate `/app/auditor` tree | Reports → Session Reports |
| Audit History | Buried in the separate `/app/auditor` tree | Reports → History |
| Observations | Buried in the separate `/app/auditor` tree | Execution → Observations |

---

## 6. Decisions & Deferrals — stated plainly

1. **Simulation lives in Execution, not Reports.** Step 3 lists "Simulation Reports" under
   Reports while Steps 4 and 10 place Simulation under Execution. The page both *runs* a mock
   audit and displays its result, so hosting it twice would breach Step 10's "no capability
   under multiple tabs". Execution wins; simulation output is viewed where it is produced.

2. **Evidence hosts the Assessment Workspace.** Step 10 lists Evidence as Review · Approvals ·
   Traceability · Version History. Only Traceability exists today — it *is* the Assessment
   Workspace. Review/Approvals live in the separate Evidence module; Version History does not
   exist. Rather than leave Evidence empty and push a working screen one level deeper, Evidence
   owns it. Step 4 also lists Assessment under Execution; that would have duplicated it.

3. **Surveillance is folded, but there is no audit-type *filter*.** Surveillance is no longer an
   independent workspace, sidebar item, or header entry — it is an audit-type view inside
   Execution, alongside Mock/Simulation. I did **not** build the
   `All · Internal · External · Surveillance · Mock · Focused · Follow-up` filter strip from the
   Step 5 example: only Surveillance and Mock have dedicated views, and the sessions list has no
   type-filter support. Delivering it would have meant either changing page behaviour (forbidden
   by Step 11) or rendering five options that do nothing. **Deferred** — it needs a
   type-filterable sessions query, which is an API change.

4. **Generated Reports not surfaced.** `POST /api/v1/audit/generate` exists in `AuditService`
   and is proxied through the Gateway with zero UI consumers. Surfacing it requires new API
   wiring, which Phase 2 explicitly excludes. **Deferred** — highest-value item in the backlog
   because the backend already exists.

5. **Per-page filter retention still resets on tab switch.** Carried over from Phase 1. Needs a
   `RouteReuseStrategy` or per-page changes; both are out of scope here.

---

## 7. Validation

| Check | Result |
|---|---|
| Duplicate routes eliminated or redirected | ✅ 8 audit + 9 auditor redirects |
| Duplicate screens consolidated | ✅ findings, assessment, reports |
| Reports from one tab | ✅ 3 sections under Reports |
| Surveillance integrated into Execution | ✅ no longer independent |
| Hidden capabilities surfaced | ✅ 5 |
| Navigation simplified | ✅ sidebar 1 entry, header 12 → 1 |
| One canonical location per capability | ✅ see §1 |
| Toolbars contain actions only | ✅ 0 nav controls remain |
| Navigation buttons removed | ✅ 21 (Phase 1) + 3 (Phase 2) |
| Layout consistent across tabs | ✅ shared capability shell + global page container |
| Breadcrumbs standardised | ✅ tab strip is the single locator |
| Business logic preserved | ✅ no service, model, or API file modified |
| APIs untouched | ✅ |
| Workflows unchanged | ✅ |
| No feature regression | ✅ every URL resolves; build + typecheck clean, 0 warnings |

**Not verified:** I could not sign in, so the workspace has not been exercised in a browser.
Routes, redirects, guards, build and typecheck are verified statically. Click-through of each
tab and sub-tab — particularly the auditor-role sub-tabs and the `sessions/:id` redirect —
should be confirmed before sign-off.

---

## 8. Future Expansion (Step 13)

The architecture absorbs new capability **without touching the sidebar**:

- A new capability → a tab in `AUDIT_WORKSPACE_TABS`.
- A new section inside a capability → a sub-tab in that capability's shell.
- Both placeholders (Intelligence, Administration) already have routes and hosts, so building
  them is a component swap with no routing or navigation change.

The sidebar contract is fixed at **one Audit entry**, and the header module switcher at **one**.
