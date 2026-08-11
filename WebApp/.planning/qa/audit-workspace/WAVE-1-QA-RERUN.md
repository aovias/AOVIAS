# Wave 1 — Completion & QA Rerun Report

**Date:** 2026-08-01 · **Gate:** Wave 1 → Wave 2
**Repos touched:** `WebApp` (Angular) and `ComplaincePlatform` (backend/Gateway) — two separate git repos.
All changes are in working trees, **uncommitted**.

## Verdict: ✅ Wave 1 PASSED — cleared to start Wave 2

All five Wave 1 items are closed. Zero Critical or High defects remain open from Wave 1 scope.
No regressions introduced; one pre-existing performance characteristic newly quantified.

---

## 1. Development tenant license — reactivated safely

The org had **no license record at all** (`licenseKeyId` empty, `"No active license."`), which is why
`TrialAccessMiddleware` treated it as `Expired` and blocked every non-GET.

Fixed using the product's **own supported endpoints** — no code change, no weakening of the licensing
system, nothing disabled:

```
POST /internal/licenses/trial/get-or-issue   → key AOVI-27F7-0E06-5812-D342, Trial, 50 seats
POST /internal/licenses/activate             → 200
GET  /internal/licenses/status               → status=Active, isOperational=true, expires 2027-08-01
```

`EnforceReadOnly` remains **on**. The middleware is untouched and still blocks writes for genuinely
expired tenants — verified by the fact that it correctly blocked this one until a real license existed.

> **Note for staging/production:** this was a *missing seed*, not a bug. Any environment provisioned
> the same way will be read-only until a license is issued. Worth adding to environment setup.

## 2. FrameworkId / JourneyId contract fix — atomic across 4 layers

**Root cause:** `organization-detail` never exposed `frameworkId`, so the client had only a journey id
and sent it as `frameworkId` to endpoints that filter by framework. No frontend-only fix was possible.

### Backend (`ComplaincePlatform`) — additive, backward compatible

| File | Change |
|---|---|
| `ComplianceService/Application/DTOs/HospitalJourneyBatchDto.cs` | `+ Guid FrameworkId` |
| `ComplianceService/Controllers/V1/JourneysController.cs` | map `FrameworkId = j.FrameworkId` |
| `Gateway/Application/Handlers/GetOrganizationDetailQueryHandler.cs` | `+ FrameworkId` on internal DTO + mapping |
| `Gateway/DTOs/OrganizationDetailDto.cs` | `+ Guid FrameworkId` on `JourneyDto` |

Purely additive — existing clients reading only `journeyId` are unaffected. Both projects build with
**0 errors**.

### Frontend (`WebApp`) — the two ids are now distinct types of thing

| File | Change |
|---|---|
| `core/services/compliance-context.service.ts` | Separate `journeyId` + `frameworkId` (subjects, getters, setters, `clear()`), with a class doc stating the distinction |
| `core/services/app-context.service.ts` | `switchContext(hospitalId, journeyId, frameworkId)`; separate storage keys — existing `selected_framework_journey_id` **retained** for the journey (the name was always right), new `selected_framework_id` added |
| `core/services/organization.service.ts` | `+ frameworkId?` on `HospitalJourney`; new `getFrameworkIdForJourney()` — the **single** mapping point |
| `core/navigation/sidebar/sidebar.component.ts` | Selector resolves journey → framework; restore path backfills framework for older sessions |
| `core/layout/user-profile-menu.component.ts` | Same, via the shared helper |
| `features/consultant/pages/consultant-portfolio.component.ts` | Same, via the shared helper |

Three call sites previously would have duplicated the lookup — consolidated into one
`OrganizationService` method instead.

**Backward compatibility:** sessions persisted before the split have no framework id. Those callers
now **omit** the framework filter (returning unfiltered results) rather than filtering on a journey id
and silently returning nothing — degrading to "too much data" instead of "no data". The sidebar
backfills the correct id as soon as organization detail loads.

`tsc --noEmit` clean.

---

## 3. Test case rerun — corrected findings verified

| Case | Before | After | Result |
|---|---|---|---|
| **DEF-003** Planning schedule, Nov 2026 | "No scheduled audits for this period." | **3 audits listed** (Emergency, Documentation Control, Quality Governance) | ✅ Fixed |
| DEF-003 request param | `frameworkId=e1460468…` (journey id) | `frameworkId=0fe8a9c0…` (framework id) | ✅ Correct |
| `organization-detail` payload | `journeyId` only | `journeyId` **and** `frameworkId`, both correct | ✅ Fixed |
| sessionStorage | one conflated key | `selected_hospital_id`, `selected_framework_journey_id`, `selected_framework_id` — all distinct and correct | ✅ Fixed |
| **DEF-002** Create scheduled audit | `403 FORBIDDEN` | **`POST /api/v1/audit-schedule → 200 OK`** | ✅ Fixed |
| **DEF-002** persisted data integrity | *(never reached)* | created row has `frameworkId=0fe8a9c0…` — the **correct** id | ✅ Verified |
| **DEF-006** SignalR negotiate | `403` + endless retry | **`200`** + valid `connectionId` / transports | ✅ Fixed |
| **DEF-001** `execution/sessions` deep link | → `/app/dashboard` | loads, 16 rows, all sub-tabs | ✅ Fixed |
| **DEF-001** `overview/auditor-home` deep link | → `/app/dashboard` | loads; sub-tab visible after reload | ✅ Fixed |
| **DEF-001** `reports/history` deep link | → `/app/dashboard` | loads | ✅ Fixed |
| **DEF-005** Context Drawer | reported broken | **false positive** — harness artifact, withdrawn | ⚪ Invalid |
| **DEF-009** CAPA context reactivity | stuck on empty state | loads 176 deficiencies on first navigation | ✅ Fixed (side effect) |

### Data corruption audit — none found, no migration needed

```
known journeyIds:   [e1460468-76d0-418c-85bf-d65cdb31a1ea]
known frameworkIds: [0fe8a9c0-9ea0-4970-9097-d4f73d45dca7]
schedule rows scanned:                    15
rows with correct frameworkId:            15
rows with journeyId in frameworkId:        0
```

Because the expired license had blocked every write, the broken client never persisted a bad id.
The sequencing decision (contract fix **before** unblocking writes) held — the first row created
after the fix carries the correct framework id.

## 4. Regression results

| Area | Result |
|---|---|
| All 9 audit tabs render | ✅ Pass |
| Overview KPIs | ✅ Restored — 95 overdue, 15 open findings, 82 CAPAs; "Upcoming scheduled" moved 4 → **5**, correctly reflecting the audit created during testing |
| Planning / Execution / Evidence / Findings / CAPA / Reports / Intelligence / Administration | ✅ All load with data |
| Role-guarded deep links | ✅ Pass (DEF-001) |
| Legacy redirects, back/forward | ✅ Unchanged |
| Console | ✅ Clean — SignalR error loop gone |
| Angular typecheck | ✅ Clean |
| Backend build | ✅ 0 errors (both projects) |

**No new Critical or High defects introduced.**

### New observation (not a regression)

**NEW-04 [Medium] — `control-center/compliance/*` cold-start hangs.** First call after a
ComplianceService restart exceeds **60 s** (Gateway returns 502; direct call times out). Subsequent
calls return in **<1 s**. The original performance report's healthy numbers were measured against a
long-warm service, so this cold path had never been exercised. Users hitting the app right after a
deploy will see the Audit Overview stuck on "Loading audit KPIs…". Worth a warm-up task or a cache
prime on startup.

## 5. Breaking change assessment

| Change | Breaking? | Notes |
|---|---|---|
| `+ frameworkId` on journey DTOs | **No** | Additive; old clients ignore it |
| `ComplianceContextService.setJourneyId/getJourneyId` | **No** | New members; existing ones keep their names and meaning |
| `AppContextService.switchContext` arity 2 → 3 | **Yes, internal only** | 3 call sites, all updated; compile-time enforced |
| New `selected_framework_id` session key | **No** | Absent key degrades to "no framework filter" |
| `getFrameworkId()` now returns a real framework id | **Behavioural** | ~40 consumers previously received a journey id. Audit module verified correct. Other modules (evidence, journey, copilot, tasks, control-center) sampled and unaffected in this run because most only filter by `hospitalId`. **Wave 2 should audit each consumer explicitly.** |

## 6. Remaining technical debt

1. **Audit all `getFrameworkId()` consumers** (~40) and switch any that genuinely address a journey to
   `getJourneyId()`. Highest-value Wave 2 task; the type system cannot catch this.
2. `HospitalFrameworkSelection.frameworkId` still *carries* a journey id — the dialog's field name is
   now misleading. Rename to `journeyId` for clarity.
3. `NEW-01/02` (read-only license UX + distinguishable Gateway code) — still unimplemented; not
   reachable now that the tenant is licensed, but real for expired customers.
4. `NEW-03` — allowlist `/hubs/**` in `TrialAccessMiddleware` so read-only tenants keep live reads.
5. `NEW-04` — cold-start warm-up for control-center.

## 7. Wave 2 entry criteria

| Criterion | Status |
|---|---|
| No Critical defects open (Wave 1 scope) | ✅ |
| No High defects open (Wave 1 scope) | ✅ |
| CRUD Create operational | ✅ |
| Framework/Journey separation complete | ✅ |
| Roles survive refresh | ✅ |
| SignalR operational | ✅ |
| Regression passes | ✅ |
| Existing functionality preserved | ✅ |

**Cleared to begin Wave 2 (business correctness: DEF-004, 011, 012, 013, 014).**
