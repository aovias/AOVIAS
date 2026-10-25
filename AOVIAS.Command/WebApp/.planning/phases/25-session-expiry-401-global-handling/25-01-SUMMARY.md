# Plan 25-01 — Summary

## Status: Complete

## Delivered

- **`SessionExpiryCoordinatorService`** (`core/session/session-expiry-coordinator.service.ts`): idempotent session end — `AuthService.logout()`, warning toast, navigate to `/login` with `sessionExpired=1` and optional `returnUrl`; `terminationInProgress` signal; mutex reset via `Router.navigate(...).finally()`.
- **`TokenRefreshService`** (`core/auth/token-refresh.service.ts`): single-flight `getFreshAccessToken$()` using `defer` + `shareReplay`; **`catchError`** on refresh for all failures (including missing refresh token); structured **`LoggerService.warn`** `{ event: 'refresh_failed', status, url }`; delegates to coordinator on failure.
- **`SKIP_AUTH_REFRESH`** (`core/http/http-context.tokens.ts`): retried requests after refresh set context so a **second 401** does not trigger another refresh.
- **`authInterceptor`**: uses `TokenRefreshService`; no `try/catch`; skips refresh for auth URLs and when `SKIP_AUTH_REFRESH`; removed `window.location`.
- **`apiResponseInterceptor`**: **401/403** logged at **debug** only; other errors still `console.error`.
- **`app.config.ts`**: comment documenting interceptor order.
- **`AuthService.logout()`**: calls **`CommonLookupService.clearCaches()`**.
- **`CommonLookupService`**: **`clearCaches()`**; **401/403** use **debug** log in `catchError`.
- **`onboarding.guard` / `onboardingEntryGuard`**: **401/403** on org load → **`UrlTree`** to **`/login`**.
- **`hospital-profile-step`**: **`isAuthHttpError`** — no misleading hospital/department strings for **401/403**.

## Build

- `ng build --configuration=development` — passed.

## Commits

- (orchestrator) Record hashes in repo history after `feat(25-01): ...` / `docs(25): ...`.
