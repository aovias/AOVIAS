# Phase 25 — Verification

**Date:** 2026-04-03  
**Status:** passed (static + build); **manual browser QA** tracked in `25-VERIFY-WORK.md`

**Latest verify-work:** `ng build --configuration=development` — **PASS** (2026-04-03). See `25-VERIFY-WORK.md` for full checklist.

## must_haves (from 25-01-PLAN)

| Criterion | Evidence |
|-----------|----------|
| Refresh failures use **`catchError`** on Observable stream | `TokenRefreshService.getFreshAccessToken$()` — `catchError` after `map`; no HTTP path uses `try/catch` for async refresh |
| Single-flight refresh | `refreshInFlight$` + `shareReplay({ refCount: true })` + `finalize` clears mutex |
| Any refresh failure → logout + optional toast + login route | `catchError` → `SessionExpiryCoordinatorService.handleRefreshFailure` / `handleSessionTerminated('no_refresh_token')` → `logout()` + `showWarning` + `navigate` |
| No refresh loop on retried 401 | Retried `HttpRequest` uses `context.set(SKIP_AUTH_REFRESH, true)`; interceptor returns `throwError` when flag set |
| Logging for refresh failures | `logRefreshFailure` → `LoggerService.warn(..., { event, status, url })` |
| No misleading feature copy for 401/403 | `HospitalProfileStepComponent.isAuthHttpError` suppresses generic strings |
| Build | `ng build --configuration=development` exit 0 |

## Manual QA (recommended)

- Force **503** on `/identity/refresh` (proxy/mock): expect logout + toast + `/login?sessionExpired=1`.
- Two parallel **401**s: expect one refresh HTTP call (network tab).
- **401** after successful refresh still **401**: expect no second refresh (SKIP_AUTH_REFRESH on retry).

## Gaps / follow-ups

- Login page optional inline message for `sessionExpired` query (not in this plan).
- E2E tests for interceptor + refresh (not added).
