# Defect Log — Audit Workspace QA Sprint

**Sprint:** Enterprise QA Validation — Release Gate before Stage 5B
**Executed:** 2026-08-01
**Build:** branch `fix/session/20260624-141124` @ `0c88979`
**Environment:** Angular dev server `http://localhost:4200` → Gateway `http://localhost:5212`
**Account:** `ekta.gupta@gmail.com` — role claim **`ORG_ADMIN`**
**Context:** org `2759fad4-c895-4a5f-9e49-6ded5d378098`, hospital `d9f5554a-bf83-4262-b386-a31587b0ba06`,
journey `e1460468-76d0-418c-85bf-d65cdb31a1ea`, framework `0fe8a9c0-9ea0-4970-9097-d4f73d45dca7`

> **Evidence note:** the browser pane in this environment does not composite frames, so bitmap
> screenshots could not be captured. Every defect below is instead evidenced by reproducible
> DOM state, computed styles, network request/response pairs, and console output — all captured
> at runtime, not by code inspection. Source references are given only to pinpoint root cause
> *after* the runtime symptom was observed.

## Status after Wave 1 remediation (2026-08-01)

| ID | Original severity | Status |
|---|---|---|
| DEF-001 Roles lost on reload | Critical | ✅ **Fixed** — `UserContextService.hydrateFromToken()` |
| DEF-002 Create 403 | Critical | ✅ **Resolved & reclassified** — not an auth bug; tenant had **no license**, `TrialAccessMiddleware` correctly enforced read-only. License issued + activated; `POST → 200` |
| DEF-003 journeyId sent as frameworkId | High | ✅ **Fixed** — `frameworkId` added to journey DTOs across ComplianceService + Gateway; ids separated in Angular. **0 corrupted rows** found |
| DEF-004 Session Type/Status blank | High | ⏳ Open — Wave 2 |
| DEF-005 Context Drawer | High | ⚪ **Withdrawn — false positive** (harness artifact; see below) |
| DEF-006 SignalR 403 loop | High | ✅ **Resolved** — same root cause as DEF-002; negotiate now `200` |
| DEF-007 Backend method names in UI | High | ⏳ Open — Wave 2/3 |
| DEF-008 No Verification/Closure | Medium | ⏳ Open — Wave 4 (net-new build) |
| DEF-009 CAPA ignores context | Medium | ✅ **Fixed** (side effect of DEF-001/003 context hydration) |
| DEF-010 – DEF-015 | Medium | ⏳ Open — Wave 2 |
| DEF-016 – DEF-019 | Low | ⏳ Open — Wave 3 |
| **NEW-01** No read-only/expired-license UX | High | ⏳ Open |
| **NEW-02** Gateway returns generic `FORBIDDEN` for license blocks | High | ⏳ Open |
| **NEW-03** `/hubs/**` not allowlisted in read-only mode | Medium | ⏳ Open |
| **NEW-04** control-center cold start >60 s | Medium | ⏳ Open |

**Open Critical: 0 · Open High: 4** (DEF-004, DEF-007, NEW-01, NEW-02).
Rerun evidence: [WAVE-1-QA-RERUN.md](WAVE-1-QA-RERUN.md).

## Original severity summary (as first reported)

| Severity | Count | IDs |
|---|---|---|
| Critical | 2 | DEF-001, DEF-002 |
| High | 5 | DEF-003 … DEF-007 |
| Medium | 8 | DEF-008 … DEF-015 |
| Low | 4 | DEF-016 … DEF-019 |
| **Total** | **19** | |

---

## CRITICAL

### DEF-001 — Role context is lost on page reload; every role-guarded audit route silently ejects the user
**Severity:** Critical **Module:** Core / Auth / Audit navigation

**Steps to reproduce**
1. Log in as `ekta.gupta@gmail.com`. Do **not** reload.
2. Navigate in-app: Audit → Execution. Sub-tabs **Active Audits, Observations, Surveillance, Mock/Simulation** are present. Open *Active Audits* → `/app/audit/execution/sessions` loads, 16 sessions listed.
3. Now press F5 (or open `http://localhost:4200/app/audit/execution/sessions` directly as a deep link/bookmark).

**Expected:** the same page loads; ORG_ADMIN is explicitly in the allowed set
(`audit-workspace-roles.ts:12-18` → `AUDITOR_ROLE_IDS` includes `'ORG_ADMIN'`).

**Actual:** the user is redirected to `/app/dashboard` with **no message**. The *Active Audits* and
*Observations* sub-tabs disappear from the Execution tab strip; `/app/audit/overview/auditor-home`
also disappears from Overview.

**Root cause:** `UserContextService` (`src/app/core/services/user-context.service.ts`) holds roles in an
in-memory `BehaviorSubject`. `setUser()` is called from exactly one place — `login.component.ts:185`.
Nothing rehydrates it from the JWT or session on bootstrap, so after any reload `getUserRoles()`
returns `[]` and `roleGuard` (`src/app/core/guards/role.guard.ts:19-24`) fails every check.

**Impact:** deep links, bookmarks, F5 and browser-restore are all broken for Sessions, Observations,
auditor-home, Reports→Session Reports and Reports→History. Any Quality Team member who refreshes
mid-audit is silently thrown out of the workspace.

**Console/network:** no error emitted — the redirect is silent.

---

### DEF-002 — Cannot create a scheduled audit: `POST /api/v1/audit-schedule` returns 403
**Severity:** Critical **Module:** Audit → Planning

**Steps to reproduce**
1. Audit → Planning → *Schedule list*.
2. Title `QA Sprint Test Audit — Aug 2026`, Date `2026-08-15`, Type `Internal`, Lead auditor `QA Bot Auditor`.
3. Click **Create scheduled audit**.

**Expected:** the audit is created and appears in *Scheduled audits*.

**Actual:** request fails.
```
POST http://localhost:4200/api/v1/audit-schedule → 403 Forbidden
{"success":false,"code":"FORBIDDEN","message":"Access forbidden", …}
```
UI shows only `Failed to create scheduled audit.` — no indication it is a permissions problem.

**Impact:** this is the entry point of the audit lifecycle. **Phase 5 (end-to-end lifecycle) cannot be
executed at all.** All downstream Create/Update/Delete testing is blocked by this single defect.

---

## HIGH

### DEF-003 — Planning "Scheduled audits" list is always empty: journey id is sent as `frameworkId`
**Severity:** High **Module:** Audit → Planning

**Steps to reproduce**
1. Audit → Planning. Set the *Scheduled audits* filter to Year `2026`, Month `11`. Click **Refresh**.
2. Observe the table.

**Expected:** 3 audits (Nissing/Emergency/Documentation Control/Quality Governance are seeded in Nov 2026).

**Actual:** `No scheduled audits for this period.`

**Proof — same endpoint, three parameterisations:**
| Query | Rows |
|---|---|
| `…/calendar?year=2026&month=11&hospitalId=…` (no framework filter) | **3** |
| `…&frameworkId=e1460468-…1ea` ← what the UI sends (this is the *journey* id) | **0** |
| `…&frameworkId=0fe8a9c0-…dca7` ← the real framework id | **3** |

**Root cause:** the framework selector is populated from journeys —
`sidebar.component.ts:1590` maps `id: j.journeyId`. The dialog result flows into
`AppContextService.switchContext()` (`app-context.service.ts:49-58`), is stored as *frameworkId* in
`ComplianceContextService` (`compliance-context.service.ts:21`) and in sessionStorage under the
tellingly-named key `selected_framework_journey_id`, then is sent verbatim as the `frameworkId`
query param by `audit-management-api.service.ts:115-121`.

**Secondary impact (data integrity):** the same conflated id is written on create —
`audit-management-api.service.ts:97` (`createScheduled`) and `audit-planning.component.ts:780/842`
(`createSession`). Once DEF-002 is fixed, every record created will be stamped with a journey id in
its `frameworkId` column. **Fix DEF-003 before DEF-002 is unblocked**, or the system will start
writing corrupt rows.

---

### DEF-004 — Audit session Type and Status columns render blank (enum casing mismatch)
**Severity:** High **Module:** Audit → Execution → Active Audits

**Steps to reproduce:** log in, Audit → Execution → *Active Audits*.

**Expected:** Type = `Internal`, Status = `In Progress`.

**Actual:** both cells are empty for all 16 rows.

**Proof:**
```
API  GET /api/v1/auditor/sessions →  { "auditType": "internalAudit", "status": "inProgress", … }
DOM  ["IA-2026-016", "", "", "Ananya Nair", "0", "2", "Workspace"]
```
Lookup maps are keyed **PascalCase** — `auditor-workspace.model.ts:226-245`
(`AUDIT_TYPE_LABELS.InternalAudit`, `SESSION_STATUS_LABELS.InProgress`) — while the API emits
**camelCase**. `AUDIT_TYPE_LABELS['internalAudit']` is `undefined`, so the cell renders empty.

**Impact:** a Quality Team cannot tell which audits are in progress, closed or submitted — the
primary operational signal of the Execution screen is missing.

**Same root cause, second symptom:** `audit-surveillance.component.ts:251` compares
`s.status === 'Closed' || s.status === 'Submitted'` against camelCase values, so the Surveillance
*Closed* KPI is permanently `0`.

---

### DEF-005 — ~~Context Drawer never becomes visible~~ — **WITHDRAWN: FALSE POSITIVE (2026-08-01)**

> **This defect is invalid. It was an artifact of the test harness, not a product fault.**
>
> The QA browser pane runs with `document.visibilityState === "hidden"` (it does not composite
> frames — the same reason screenshots were unavailable). **CSS transitions do not advance in a
> hidden page.** `visibility` and `transform` are both transitioned on `.drawer`, so they stayed
> frozen at their pre-transition (closed) values no matter how many times the drawer was opened —
> which is exactly what I measured and misread as a defect.
>
> **Proof the cascade is correct.** With the transition disabled, the `.is-open` rule applies
> immediately and correctly:
> ```
> element.style.transition = 'none'
> → visibility: visible
> → transform: matrix(1,0,0,1,0,0)   // translateX(0)
> → boundingRect.x = 1072            // 1440 − 368: correctly docked to the right edge
> → innerText = "Context | 📍 Pin context open | ✕ Close context | ✦ AI guidance | ◈ Context |
>                ⧉ Related 8 | LIFECYCLE | Status | 27 findings | To advance: …"
> ```
> The drawer, its header, pin/close controls, section rail and body content all render correctly.
> **No code change was made, and none is needed.**
>
> Lesson for future runtime QA in this environment: any assertion about a *transitioned* or
> *animated* property is unreliable here. Assert on DOM/class state, or disable the transition
> first. Non-animated computed values (e.g. the dark-theme colour measurements in DEF-Theme) are
> unaffected and remain valid.

**Original (incorrect) report retained below for traceability.**

**Severity:** ~~High~~ → **Invalid** **Module:** Audit workspace shell (all tabs)

**Steps to reproduce**
1. Load `http://localhost:4200/app/audit/findings` at 1440×900.
2. Click the **Context** button in the workspace rail.

**Expected:** the 368px context panel slides in from the right.

**Actual:** the backdrop/scrim renders and dims the page, but the panel remains invisible.
Reproduced on a clean page load and across repeated toggles.

**Computed state after opening:**
```
class      = "drawer is-open"          ← open class IS applied
visibility = hidden                     ← should be visible
transform  = matrix(1,0,0,1,368,0)      ← translateX(100%), i.e. fully off-screen
boundingRect.x = 1440 (viewport width)  ← sits past the right edge
innerText  = "" (not rendered)
elementFromPoint at the panel's edge → "scrim"
```
The panel's content *is* in the DOM (`drawer-body` textContent =
`"Lifecycle · Status · 27 findings · To advance: Every open finding is promoted to CAPA or closed"`),
so this is purely a presentation failure. The intended rule
`.drawer:not(.is-pinned).is-open { transform: translateX(0); visibility: visible; }`
(`context-drawer.component.ts:155-158`) is present in the stylesheet but is not winning at runtime.

**Impact:** Context, AI guidance, Related and Activity — the entire Phase 7 workspace-experience
surface — are unreachable. The scrim also dims the page with no visible way to dismiss it.

---

### DEF-006 — SignalR hub negotiation fails with 403 and retries in a loop
**Severity:** High **Module:** Core / real-time

**Actual:** repeated console errors from app start onward:
```
Error: Failed to complete negotiation with the server: Error:
{"success":false,"code":"FORBIDDEN","message":"Access forbidden", …}: Status code '403'
Error: Failed to start the connection: …
```
**Impact:** all live/push updates (notification badges, activity stream, collaborative status) are
dead, and the client retries indefinitely, producing continuous console noise and wasted requests.
No user-facing degradation notice is shown.

---

### DEF-007 — Executive Intelligence leaks backend implementation identifiers into the UI
**Severity:** High (professional credibility / information disclosure) **Module:** Audit → Intelligence

**Steps to reproduce:** Audit → Intelligence.

**Actual:** KPI cards display internal .NET service and method names as their user-facing subtitles:
- `COMPLIANCE READINESS 37%` — subtitle **`IComplianceScoringService.CalculateFrameworkScoreAsync`**
- `ACCREDITATION READINESS 37%` — subtitle **`IComplianceScoreService.GetJourneyScoreAsync`**

**Expected:** a human-readable descriptor (e.g. "Weighted framework score across all chapters").

**Impact:** this is an executive-facing screen. It exposes internal architecture and reads as
unfinished to any hospital stakeholder or assessor.

---

## MEDIUM

### DEF-008 — Lifecycle has no Verification or Closure stage
**Module:** Audit workspace (in-scope per QA brief)
Tab strip and workflow status bar both run **Planning → Execution → Evidence → Findings → CAPA → Reports**.
The QA scope requires **Verification** and **Closure** as distinct stages; neither exists as a tab, a
route (`src/app/features/audit/routes.ts`), or a workflow step. Exit criterion "Complete lifecycle
validated" cannot be met.

### DEF-009 — CAPA screen does not react to context selection
**Module:** Audit → CAPA
With no hospital/framework chosen, CAPA correctly shows `Select a hospital and framework to view
deficiencies.` After confirming the selector dialog, the screen **stays** on that empty state.
Navigating away and back loads it correctly (176 deficiencies). The page does not subscribe to
context changes.

### DEF-010 — CAPA-specific counters are all zero while 176 deficiencies exist
**Module:** Audit → CAPA
Observed KPI row: `176 TOTAL DEFICIENCIES · 164 OPEN · 12 RESOLVED · 82 BREACHED · 82 ESCALATED`
but **`0 OPEN CAPAS · 0 OVERDUE CAPAS · 0 CAPA ≤30D · 0 CAPA 31–60D · 0 CAPA 61–90D · 0 CAPA >90D`**,
and `0 SLA ON TRACK · 0 SLA WARNING · 0 AT RISK` against 164 open items. The SLA buckets do not sum
to the open count and the CAPA entity appears unwired.

### DEF-011 — Findings `SOURCE` column renders `0`; `OWNER` renders a raw GUID
**Module:** Audit → Findings
Every row shows Source `0` and Owner `96de7cba-eddf-4052-b04c-42b55caa3604`. The dashboard's
*BY SOURCE* breakdown likewise shows a blank-labelled bucket with count 24.
**This originates in the API, not the UI** — `GET /api/v1/findings/workspace` returns
`"source": "0"` and `"owner": "<userGuid>"` with no display name. Data-contract defect.

### DEF-012 — Cross-screen KPI disagreement for open findings
**Module:** Audit → Overview vs Findings
Overview *Open findings* = **15**; Findings Workbench = **27 TOTAL / 11 OPEN**. Two screens in the same
workspace report different figures for the same measure with no visible filter difference.

### DEF-013 — Reports → Session Reports renders raw enum values
**Module:** Audit → Reports
The table prints `internalAudit`, `inProgress`, `closed` verbatim. Note this is a *different* wrong
behaviour from DEF-004 (which blanks the same values) — the two screens handle one enum inconsistently.

### DEF-014 — Audit Reports defaults to a period that yields an empty report
**Module:** Audit → Reports
Default period *This Month* gives `0 CRITICAL / 0 MAJOR / 0 MINOR / 0 TOTAL` and
`No findings in selected period.`, while 27 findings exist dated Jul 13 2026. The landing state of the
reporting screen is empty for a user with a full year of data.

### DEF-015 — Accreditation standard text contains corrupted characters
**Module:** Audit → Evidence (Assessment Workspace) — content/data
Standard titles render as `The organisation de?nes and displays…`, `well-de?ned registration…`.
Verified as a literal U+003F `?` in the payload (char codes `64 65 3f 6e 65 73`), i.e. the `fi`
ligature was lost during NABH content ingestion — not a rendering fault. Affects the standard text an
auditor reads on every objective element.

---

## LOW

### DEF-016 — Wrong-password error reads "Authentication required"
**Module:** Login. A 401 from `POST /identity/login` surfaces the snackbar `Authentication required`
instead of "Invalid email or password", which reads as a session problem rather than a typo.

### DEF-017 — Hospital & Framework dialog: Confirm disabled with no explanation
**Module:** Shell / context selector. With exactly one hospital and one framework available, the
hospital is pre-selected but the framework is not, so **Confirm Selection** is disabled on open with
no hint that a framework must be picked.

### DEF-018 — Duplicate API calls on load
**Module:** Core. Measured over one session: `copilot/personas/active` ×6,
`copilot/experience/workspace` ×6, `role-experience/home` ×3, `auditor/sessions` ×3,
`audit-schedule/calendar` ×2. `role-experience/home` is also the slowest endpoint (854 ms), so it is
being paid for three times.

### DEF-019 — Audit routes never set the document title
**Module:** Audit routing. Login sets `Sign In | AOVIAS Command`, the dashboard sets
`Compliance Dashboard`, but every `/app/audit/**` route leaves the generic
`AOVIAS Command - Healthcare Intelligence Platform`. Breaks tab identification and browser history.

---

## Observations (not defects)

- **Tokens in localStorage.** `access_token` and `refresh_token` are stored in `localStorage`, making
  them readable by any XSS. Worth a deliberate decision (httpOnly cookie vs. accepted risk) before
  production, but it is a design choice, not a regression.
- **Audit → Administration** is an honest placeholder: *"This capability is not built yet. Existing
  audit functionality is unaffected."* Audit types, Checklist templates, Prep templates and Audit
  teams are all unimplemented. Recorded as a scope gap, not a defect.
