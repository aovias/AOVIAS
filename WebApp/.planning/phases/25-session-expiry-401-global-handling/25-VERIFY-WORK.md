# Phase 25 — Verify-work (manual acceptance + automated smoke)

**Run:** 2026-04-03  
**Scope:** Session expiry, token refresh single-flight, 401 handling, guards, onboarding hospital profile UX.

---

## 1. Automated / static (executed)

| Check | Result |
|-------|--------|
| `ng build --configuration=development` | **PASS** (exit 0) |
| `TokenRefreshService.getFreshAccessToken$()` uses `catchError` (no `try/catch` for HTTP refresh) | **PASS** — see `token-refresh.service.ts` |
| `SKIP_AUTH_REFRESH` on retried request | **PASS** — `auth.interceptor.ts` sets `context: req.context.set(SKIP_AUTH_REFRESH, true)` |
| Interceptor skips refresh when `SKIP_AUTH_REFRESH` or auth endpoints | **PASS** — lines 43–48 |
| `SessionExpiryCoordinatorService`: logout + toast + `navigate` + `finally` mutex reset | **PASS** — `session-expiry-coordinator.service.ts` |
| `HospitalProfileStepComponent.isAuthHttpError` for departments + hospitals | **PASS** — `hospital-profile-step.component.ts` |
| `CommonLookupService` 401/403 → `logger.debug` in `catchError` | **PASS** — `common-lookup.service.ts` |

---

## 2. Manual QA (browser / network) — operator checklist

Complete these in a dev environment against a real or mocked API.

| # | Scenario | Steps | Expected |
|---|----------|--------|----------|
| M1 | Refresh returns **503** | Log in; block or mock `POST …/identity/refresh` → **503** (e.g. dev proxy); trigger any **401** on a protected API | One **“Session expired…”** toast; redirect to **`/login`** with **`sessionExpired=1`**; tokens cleared; **no** hung spinner |
| M2 | Parallel **401**s | Open DevTools Network; from console or two components fire two authenticated calls that return **401** simultaneously | **One** `refresh` request (not two) in Network tab |
| M3 | **401** after refresh still **401** | Mock: refresh **200**, then original resource still **401** | **No** second refresh call for that request chain (retry carries `SKIP_AUTH_REFRESH`) |
| M4 | Idle session | Log in; wait until access token invalid server-side (or force **401**) | Redirect to login; **no** “Failed to load departments (timeout or network…)” for **401/403** on hospital profile |
| M5 | Onboarding guard | While logged in with expired session, navigate to a route protected by **`onboardingGuard`** | Redirect to **`/login`** with **`returnUrl`**, not stuck on onboarding with bad state |
| M6 | Header logout | Use app **Logout** (header) | Same as before: caches cleared; no stale departments after next login |

**Log verification (optional):** On refresh failure, console should show **`[WARN] Refresh token request failed`** with object **`{ event: 'refresh_failed', status, url }`** (no refresh token value).

---

## 3. Known follow-ups (not blocking)

- Login screen inline copy when `?sessionExpired=1` (optional UX).
- Automated E2E for interceptor (not in Phase 25 scope).

---

## 4. Sign-off

| Role | Name | Date | Pass |
|------|------|------|------|
| Developer smoke (build + code review) | Cursor / automated | 2026-04-03 | Yes |
| Manual QA (section 2) | _ | _ | Pending |

When section 2 is complete, set **`25-VERIFICATION.md`** manual QA lines to done or attach test evidence here.
