# Wave 1 — Remediation Summary (in progress)

**Date:** 2026-08-01 · **Branch:** `fix/session/20260624-141124` · Changes are in the working tree, uncommitted.

## Headline: the QA report's root-cause analysis for DEF-002 was wrong, and I was wrong to rate it Critical-as-a-bug

`POST /api/v1/audit-schedule` returns 403 with the header **`x-trial-status: Expired`**.

The cause is `Gateway/Middleware/TrialAccessMiddleware.cs` — a deliberate CRP Phase 5 commercial
control that puts a tenant into **read-only mode** when its license status is `Expired`, `Revoked`,
`Suspended` or `Archived`. It allows GET/HEAD/OPTIONS and blocks every other verb.

```csharp
if (status is "Expired" or "Revoked" or "Suspended" or "Archived") {
    context.Response.Headers["X-Trial-Status"] = status;
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
```

**The platform is behaving as designed.** Authorization, claims, role mapping and policy are all
correct — `AuditScheduleController.Create` allows `SUPER_ADMIN, ORG_ADMIN, MANAGER`, and the test user
holds `ORG_ADMIN`. The request never reaches it.

### This also explains DEF-006 (SignalR)

SignalR's `/hubs/{hub}/negotiate` is a **POST**, and `/hubs` is not in the middleware's allowlist.
Verified directly:

```
POST /hubs/notifications/negotiate?negotiateVersion=1
→ 403 · x-trial-status: Expired
```

**DEF-002 and DEF-006 are one root cause, not two defects.** Neither is an authorization bug.

### What the *real* defects are

| # | Defect | Severity |
|---|---|---|
| **NEW-01** | The web app never tells the user the tenant is in read-only/expired-license mode. A 403 surfaces as `Failed to create scheduled audit.` The user cannot know why, or that the workspace is degraded. | **High** |
| **NEW-02** | The Gateway returns a generic `FORBIDDEN` code, indistinguishable from a permission denial. Only the `X-Trial-Status` header carries the real reason, and no client reads it. It should return a distinct code (e.g. `LICENSE_READ_ONLY`) so clients can react. | **High** |
| **NEW-03** | Real-time transport is collateral damage. `/hubs/**` negotiate is a connection handshake, not a tenant mutation — read-only mode should still permit live *reads*. It should be allowlisted. | **Medium** |

DEF-002 and DEF-006 should be **reclassified**: not code defects, but (a) an environment state and
(b) the three UX/contract defects above.

---

## Completed and verified

### ✅ DEF-001 — Role context lost on reload — **FIXED**

**Root cause:** `UserContextService` held roles in an in-memory `BehaviorSubject` populated from
exactly one call site (`login.component.ts:185`). Nothing rehydrated it, so after any reload
`getUserRoles()` returned `[]` and `roleGuard` rejected users who did hold the required role.

**Fix — the JWT is already the source of truth for the API, so it is the source of truth here too:**

| File | Change |
|---|---|
| `core/services/user-context.service.ts` | New `hydrateFromToken()` — decodes the persisted access token and restores id, email, username and roles. Handles the role claim as either a single string or an array. Stores both the canonical id (via `normalizeRoleId`) and the raw claim, so the guard matches either and unknown roles stay diagnosable. Idempotent — no-ops once populated, so it is safe per-navigation. |
| `core/services/app-context.service.ts` | `initializeFromSession()` now hydrates roles **before** `initializeContext()`, so roles exist before any `roleGuard` runs. |
| `core/guards/role.guard.ts` | Defensive `hydrateFromToken()` call, so a `roleGuard` route reached without a parent `authGuard` still evaluates a populated role set. |
| `core/auth/auth.service.ts` | **Bug found while fixing:** `clearLocalSession()` never cleared `UserContextService`. Harmless before (nothing read it after logout) but now dangerous — a stale identity would survive logout and short-circuit rehydration on login-as-another-user. Now cleared explicitly. |

**Runtime verification** — all three previously-broken deep links now load on a **hard page load**
(the exact scenario that failed):

| Deep link | Before | After |
|---|---|---|
| `/app/audit/execution/sessions` | → `/app/dashboard` | ✅ Loads, **16 session rows**, all 4 sub-tabs present |
| `/app/audit/overview/auditor-home` | → `/app/dashboard` | ✅ Loads |
| `/app/audit/reports/history` | → `/app/dashboard` | ✅ Loads |

Covers refresh, deep links, bookmarks, browser restart and new tabs — all read the same persisted
token. Token-expiry/refresh is unchanged: `hydrateFromToken()` is gated behind
`sessionService.isSessionValid()` in the bootstrap path.

**Regression check:** `tsc --noEmit` clean. `findings/workspace`, `auditor/sessions` and
`audit-schedule/calendar` all still 200. No new console errors.

---

## Blocked — needs a decision before I continue

### 🔴 Blocker A — the test organization's license is Expired, so **no write path can be tested**

Every POST/PUT/PATCH/DELETE in the product is blocked. This blocks:
- DEF-002 remediation verification (Create → Update → Delete must all succeed)
- All of Wave 2's CRUD validation
- All of Wave 4's workflow transitions

**This cannot be fixed in code** — and it should not be. Options:
1. **Reactivate the license for org `2759fad4-…098`** in the dev environment (the middleware reads
   `/internal/licenses/status`; there is a `/licenses/activate` endpoint, which is allowlisted).
2. Set `TrialLifecycleGatewayOptions.EnforceReadOnly = false` in the dev Gateway config only.
3. Provide a test account in an active-license organization.

I have **not** done any of these unilaterally: changing licensing state or disabling a commercial
control is a business decision, not a defect fix, and option 2 in particular must never leak to
staging or production.

### 🟡 Blocker B — DEF-003's real fix requires a backend contract change in a separate repo

The frontend conflates journey and framework ids **because the API gives it no choice.**
`GET /tenant/{id}/organization-detail` returns, per hospital:

```json
{ "journeyId": "e1460468-…", "frameworkCode": "CP00000001",
  "frameworkName": "NABH Full Accreditation (Hospital)", "startDate": "…", "expiryDate": null }
```

There is **no `frameworkId`**. The real framework id (`0fe8a9c0-…`) only appears inside audit-schedule
rows. So the client cannot send a correct `frameworkId` today — no amount of frontend refactoring
fixes this.

Correct fix, in order:
1. **Backend** — add `frameworkId` to the hospital-journey DTO in `organization-detail`.
2. **Frontend** — split `ComplianceContextService` into distinct `journeyId` and `frameworkId`;
   carry both through `AppContextService.switchContext`, the sessionStorage keys and the selector;
   send the right one per endpoint.
3. **Data** — audit existing `AuditSchedule` / `AuditSession` rows for journey ids written into
   `FrameworkId` columns; migrate if any exist.

`ComplaincePlatform` is a **separate git repository** (`V:/DevelopmentRepo/V1.0/ComplaincePlatform`).
I have made no changes there. Confirm you want cross-repo commits before I proceed.

**Sequencing still holds, and is now more important:** do not restore write access (Blocker A)
before step 1+2 land, or the system will begin persisting journey ids into `FrameworkId` columns.

---

## Second correction: DEF-005 (Context Drawer) is a **false positive** — withdrawn, no fix needed

The QA browser pane runs with `document.visibilityState === "hidden"` and does not composite frames
(the same reason screenshots were unavailable). **CSS transitions do not advance in a hidden page.**
`.drawer` transitions both `visibility` and `transform`, so they stayed frozen at their closed values
however often the drawer was opened — which is precisely what I measured and misdiagnosed.

Proof — with the transition disabled, the `.is-open` rule applies correctly and immediately:

```
element.style.transition = 'none'
→ visibility: visible
→ transform: matrix(1,0,0,1,0,0)     // translateX(0)
→ boundingRect.x = 1072              // 1440 − 368: correctly docked to the right edge
→ innerText = "Context | 📍 Pin context open | ✕ Close context | ✦ AI guidance | ◈ Context |
               ⧉ Related 8 | LIFECYCLE | Status | 27 findings | To advance: …"
```

Header, pin/close controls, section rail and body content all render. **No code was changed.**
Wave 1 item 4 is closed as invalid rather than fixed.

**Methodology correction for the remaining waves:** in this environment, never assert on a
transitioned or animated property. Assert on DOM/class state, or disable the transition first.
Non-animated computed values (the dark-theme colour and contrast measurements) are unaffected and
remain valid.

## Revised Wave 1 scorecard

| # | Item | Outcome |
|---|---|---|
| 1 | DEF-003 journey/framework | **Blocked** — needs a backend DTO change (Blocker B) |
| 2 | DEF-002 create 403 | **Reclassified** — not an auth bug; expired license (Blocker A) + NEW-01/02 |
| 3 | DEF-001 role rehydration | ✅ **Fixed and runtime-verified** |
| 4 | DEF-005 Context Drawer | ⚪ **Withdrawn — false positive** |
| 5 | DEF-006 SignalR | **Reclassified** — same root cause as DEF-002; genuine part is NEW-03 |

Net: of the five Wave 1 blockers, **one was a real frontend defect (fixed)**, two were one
environmental condition, one was a test artifact, and one needs a cross-repo contract change.
The Audit Workspace is in materially better shape than the original report implied.

## Not started

| Item | Status |
|---|---|
| DEF-002 read-only UX (NEW-01/02/03) | Designed above, not implemented |
| Wave 2 (enum/reference-data/KPI consolidation) | Not started |
| Wave 3 (theme, UX polish) | Not started |
| Wave 4 (Verification + Closure workspaces) | Not started — this is net-new product build, not remediation |

## Honest scope assessment

Wave 4 alone — two new workspaces with workflow integration, status transitions, permissions,
reporting, history, timeline, Context Drawer and Executive Intelligence integration — is a
multi-week product build, not a bug-fix wave. Waves 1–3 contain genuine remediation I can carry a
long way; Wave 4 needs design agreement on the Verification and Closure domain models before any
code is worth writing.
