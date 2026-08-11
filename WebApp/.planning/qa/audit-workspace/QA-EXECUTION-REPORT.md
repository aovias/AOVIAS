# QA Execution Report — Audit Workspace

**Release gate before Stage 5B (Enterprise Administration Expansion)**

| | |
|---|---|
| Executed | 2026-08-01 |
| Build | `fix/session/20260624-141124` @ `0c88979` |
| Environment | `ng serve` :4200 → Gateway :5212 (live backend, real data) |
| Account | `ekta.gupta@gmail.com` — single role: **ORG_ADMIN** |
| Method | Runtime testing through the browser. No mocks, no stubs, normal login flow. |
| Defects | **19** — 2 Critical, 5 High, 8 Medium, 4 Low |
| **Verdict** | 🔴 **NO-GO** |

Full detail for every defect: [DEFECT-LOG.md](DEFECT-LOG.md).

---

## 1. Verdict

**The Audit Workspace does not pass the release gate. Stage 5B must not begin.**

The decisive finding is not the count of defects — it is that **the audit lifecycle cannot be
executed even once**. Creating a scheduled audit returns `403 Forbidden` for the only account
provided (DEF-002), and the lifecycle has no Verification or Closure stage to execute even if it
did (DEF-008). The mandatory exit criterion *"Complete lifecycle validated"* is therefore not
merely failed — it is currently untestable.

Two further defects would independently justify a No-Go:

- **DEF-001 (Critical)** — pressing F5 anywhere in the workspace silently ejects the user to the
  dashboard, because roles live only in memory and are never rehydrated after login. Deep links and
  bookmarks are broken by the same cause.
- **DEF-003 (High)** — the app conflates *journey id* with *framework id*. Today this makes the
  Planning schedule permanently empty; the moment DEF-002 is fixed, the same bug will begin
  **writing corrupt `frameworkId` values** on every created audit and session.

> **Sequencing warning.** DEF-003 must be fixed **before** DEF-002 is unblocked. Fixing the 403
> first turns a read-only display bug into a data-integrity bug.

---

## 2. Coverage — what was and was not tested

Honest statement of scope, because two constraints limited this sprint.

| Phase | Status | Notes |
|---|---|---|
| 1 — Authentication | ✅ Complete | Login, invalid creds, empty-form validation, session persistence, tenant/org/hospital resolution, deep-link guard behaviour. |
| 2 — Navigation | ✅ Complete | All 9 tabs, sub-tabs, legacy redirects, back/forward, deep links. |
| 3 — Screen verification | ✅ Complete | Every audit screen opened and inspected. |
| 4 — CRUD | ⚠️ **Partial — blocked** | Create tested → 403 (DEF-002). Read tested throughout. **Update / Delete / Cancel / Undo / Bulk / Import untested** — unreachable without Create. Export CSV buttons present but downloads not exercised. |
| 5 — Full lifecycle | ❌ **Blocked** | Cannot start at step 1. Verification and Closure stages do not exist. |
| 6 — Role-based | ⚠️ **Partial** | Only ORG_ADMIN credentials were supplied. Quality Head, Auditor, Department Head, Compliance Officer and Hospital Administrator **were not tested at runtime.** Guard logic was read to explain observed behaviour only. |
| 7 — Workspace experience | ⚠️ Partial | Context Drawer is non-functional (DEF-005), which blocks AI Panel / Documents / History / Related / Timeline verification. Search and filters verified working. |
| 8 — Forms & validation | ⚠️ Partial | Required-field and empty-submit validation verified. Boundary values, duplicates, large text, special characters and date rules not reachable while Create is blocked. |
| 9 — Error handling | ⚠️ Partial | 401, 403, validation failures and empty datasets observed live. Network-offline, timeout and 5xx **not simulated**. |
| 10 — Responsive & theme | ✅ Complete | Desktop 1440/1280, tablet 768. Light and dark both inspected. |
| 11 — Performance | ✅ Complete | Full timing capture. |
| 12 — Regression | ✅ Complete | Prior-phase surfaces re-exercised. |

**Two constraints to note for the record:**

1. **Only one role's credentials were provided.** Phase 6 cannot be signed off from a single
   ORG_ADMIN account. Five further logins are needed.
2. **Screenshots could not be captured.** The browser pane in this environment does not composite
   frames, so `screenshot` fails. The "Screenshot Pack" deliverable is therefore **not produced**.
   In its place every defect carries reproducible DOM state, computed styles, and network
   request/response pairs — captured at runtime, which is stronger evidence than a bitmap for the
   defects found here.

---

## 3. Functional Test Report

### Authentication — pass with one cosmetic issue
Login succeeds against the live Gateway; JWT carries `organization_id`, `hospital_id`, `session_id`
and role `ORG_ADMIN`. Tenant, organization and hospital context all resolve. Empty-form submit is
correctly blocked (submit disabled). Invalid credentials are correctly rejected (401) but the
message is misleading (DEF-016).

### Navigation — pass, with one broken destination
All nine capability tabs resolve. `/app/audit` → `/app/audit/overview/programme`. Legacy redirects
verified live: `/corrective-actions` → `/capa`, `/assessment` → `/evidence`. Browser back restores
the correct route and content. **However**, Planning's primary CTA **"Start the audit →"** points at
`/app/audit/execution/sessions`, which under DEF-001 conditions silently dumps the user on the
dashboard — a headline call-to-action that dead-ends.

### Screens
| Screen | Result |
|---|---|
| Overview / Programme | Loads, live KPIs, activity stream. Figures disagree with Findings (DEF-012). |
| Planning | Renders; schedule list permanently empty (DEF-003); Create fails 403 (DEF-002). |
| Execution / Active Audits | 16 sessions; Type + Status blank (DEF-004). |
| Execution / Surveillance | Loads; *Closed* KPI stuck at 0 (DEF-004 root cause). |
| Evidence (Assessment) | Chapter→Standard→OE tree loads well. Corrupted standard text (DEF-015). |
| Findings | Strongest screen — 27 findings, working KPIs, search and filters. Source/Owner columns unusable (DEF-011). |
| CAPA | 176 deficiencies once loaded, but ignores context changes (DEF-009) and all CAPA counters are 0 (DEF-010). |
| Reports | Renders; raw enums (DEF-013); default period empty (DEF-014). |
| Intelligence | Renders; leaks backend method names (DEF-007). |
| Administration | Explicit "not built yet" placeholder. |

### CRUD
- **Create** — exercised on Planning. Client-side validation correct ("Title is required."). Server
  rejects with 403 (DEF-002).
- **Read** — works across every screen; no silent failures observed.
- **Update / Delete / Bulk / Import** — **not reachable.** Not tested.
- **Search** — verified working: typing `Radiology` in Findings filtered 25 rows → 2 correct matches.
- **Filters** — Year/Month filter on Planning issues the correct request and re-renders.

---

## 4. UI/UX Review

**Good.** The workspace shell is coherent: a persistent tab strip, a workflow status bar that
correctly advances Planning → … → Reports as you move, consistent table treatment, and genuinely
useful empty states ("No surveillance schedules yet. Create one from Planning with type
Surveillance." tells the user what to do next). Tables are wrapped in themed scroll containers.

**Problems.**
- The Context Drawer, a headline feature, never appears (DEF-005).
- Enum values reach the user in three different broken forms: blank (DEF-004), raw camelCase
  (DEF-013), and a bare `0` (DEF-011). The same underlying data is presented inconsistently across
  three screens.
- Backend identifiers are shown to executives (DEF-007).
- Error messaging is generic. A 403 surfaces as "Failed to create scheduled audit."; a bad password
  surfaces as "Authentication required."
- The workflow status bar is absent on Intelligence but present elsewhere.
- Audit tab links expose no accessible name on the anchor for two entries (`Findings`, `Deficiencies / CAPA`
  carry `aria-label`; the visible tab strip anchors carry text only) — minor inconsistency for AT users.

---

## 5. Responsive Testing Report

| Viewport | Result |
|---|---|
| Desktop 1440×900 | ✅ Pass. No horizontal document overflow. |
| Laptop 1280×720 | ✅ Pass. |
| Tablet 768×1024 | ✅ Pass. `documentElement.scrollWidth === clientWidth === 768` — no page-level horizontal scroll. |

Wide tables are handled correctly: the 13-column Findings grid measures 1283px inside a 631px
container with `overflow-x: auto` on `.aov-table-wrap` and scrolls independently. The tab strip
likewise scrolls internally (868px in 681px) rather than breaking the layout. **No overlapping
controls were found at any tested width.**

## 6. Theme Testing

🔴 **Fail.**
- **No theme toggle exists anywhere in the UI.** `data-theme="light"` is set on `<html>`, but no
  control switches it and no preference is persisted. Users cannot reach dark mode at all.
- Forcing `data-theme="dark"` reveals the implementation is incomplete:
  - `<body>` background stays light — `rgb(247,249,252)` — while `<main>` goes dark `rgb(15,23,42)`.
  - `<main>` inherits dark text `rgb(17,24,39)` on that dark background — effectively invisible.
  - Measured tab-label contrast: **3.03:1** (`rgb(107,114,128)` on `rgb(30,41,59)`) and **3.25:1** for
    the active tab. WCAG AA requires **4.5:1** for normal text. Both fail.

Dark theme should be treated as **not implemented** rather than defective, and the exit criterion
"Dark and light themes verified" recorded as failed.

## 7. Performance Report

✅ **Pass — performance is not a blocker.**

| Metric | Value |
|---|---|
| DOMContentLoaded | 129 ms |
| Load event | 130 ms |
| API calls per session | 30 |
| Median API duration | 358 ms |
| Mean API duration | 350 ms |
| Slowest call | `role-experience/home` — 854 ms |
| Calls exceeding 1 s | **0** |

Tab switching, drawer toggling, search and filtering were all visually immediate. Large tables
(176 deficiencies, 27 findings, 16 sessions) render without lag.

**The one inefficiency worth fixing** is redundant fetching (DEF-018): `copilot/personas/active` and
`copilot/experience/workspace` are each requested **six** times per session, `role-experience/home`
three times — and that last one is the slowest endpoint in the app, so its cost is paid triple.
De-duplicating these is straightforward and would measurably improve first paint.

## 8. Regression Report

✅ **No regressions detected** in previously completed work.

| Area (prior phase) | Result |
|---|---|
| Workspace shell + tab consolidation (Ph 61) | ✅ Intact |
| Legacy route redirects (Ph 61) | ✅ All verified live |
| Executive Intelligence (Ph 40) | ✅ Renders — but see DEF-007 |
| Role dashboards / role-experience | ✅ Loads |
| Search & filters (Findings) | ✅ Working |
| Reports surfaces | ✅ Render |
| Themed scrollbars (Ph 43) | ✅ Applied |
| Hospital/framework selector (Ph 29-30) | ✅ Functions — see DEF-017 |

Every defect in this report is a **pre-existing or newly-surfaced** issue, not a break in
previously signed-off behaviour. Note that most prior phases were signed off as
"implementation complete — human runtime signoff pending"; this sprint is, in effect, that deferred
runtime signoff, which is why so much surfaces at once.

---

## 9. Exit Criteria

| Criterion | Status |
|---|---|
| All Audit screens tested | ✅ |
| Every CRUD operation verified | ❌ Create blocked; Update/Delete/Bulk unreachable |
| Complete lifecycle validated | ❌ Cannot start; Verification & Closure absent |
| Role-based access verified | ❌ Only ORG_ADMIN available |
| Navigation verified | ⚠️ Works, but a primary CTA dead-ends |
| Context Drawer verified | ❌ Non-functional |
| Search and filters verified | ✅ |
| Validation rules verified | ⚠️ Partial |
| Error handling verified | ⚠️ Partial — messages too generic |
| Responsive behaviour verified | ✅ |
| Dark and light themes verified | ❌ Dark theme unusable, no toggle |
| Performance acceptable | ✅ |
| Regression testing passed | ✅ |
| No Critical defects remain | ❌ 2 open |
| No High severity defects remain | ❌ 5 open |
| Medium/Low documented with remediation plan | ✅ |

**5 of 15 criteria met.** Gate not cleared.

---

## 10. Recommended remediation order

**Wave 1 — unblocks all further QA (must precede any Stage 5B work)**
1. **DEF-003** — separate `journeyId` from `frameworkId` end-to-end. *Do this first, before DEF-002.*
2. **DEF-002** — grant `audit-schedule` write permission to the appropriate roles, or correct the
   role mapping; surface 403s as permission errors, not generic failures.
3. **DEF-001** — rehydrate `UserContextService` from the JWT/session on app bootstrap.
4. **DEF-005** — restore Context Drawer visibility.

**Wave 2 — correctness before re-test**
5. DEF-004 / DEF-013 / DEF-011 — normalise enum + reference-data mapping in one pass (add
   case-insensitive resolution; ask the Gateway for `source` labels and `ownerName`).
6. DEF-006 — fix hub authorization; add a bounded retry and a degraded-mode notice.
7. DEF-007 — replace backend method names with real descriptions.
8. DEF-009 / DEF-010 / DEF-012 / DEF-014 — context reactivity and KPI reconciliation.

**Wave 3 — before production**
9. DEF-008 — decide whether Verification and Closure are in scope for v1; if so they are net-new
   build, not a fix.
10. Dark theme: ship a toggle and complete the token coverage, or explicitly defer it and drop the
    exit criterion.
11. DEF-015 — re-ingest NABH standard text with correct ligature handling.
12. DEF-016 / DEF-017 / DEF-018 / DEF-019.

**Then re-run this sprint** with credentials for all six roles, and with Phases 4, 5, 6, 8 and 9
executed in full — they could not be completed this time.

---

## 11. Go / No-Go

> ### 🔴 NO-GO for Stage 5B
>
> The Audit Workspace has a sound shell, good performance and solid responsive behaviour — the
> foundation is real. But its core operational promise is unproven: **no audit can currently be
> created, and therefore no audit lifecycle has ever been run end to end.** Add a reload that
> ejects users, an invisible Context Drawer, and an id conflation poised to write bad data as soon
> as writes are enabled, and the module is not yet at enterprise-grade stability.
>
> Recommendation: complete **Wave 1** remediation, obtain credentials for the five untested roles,
> and re-run Phases 4, 5, 6, 8 and 9 before reconsidering the gate.
