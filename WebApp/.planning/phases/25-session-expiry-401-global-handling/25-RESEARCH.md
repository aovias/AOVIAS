# Phase 25 — Research: Global session expiry, 401 & refresh-token resilience

## RESEARCH COMPLETE

### Problem (observed)

- When the access session is invalid server-side (idle timeout, revoked token), APIs return **401**.
- UI surfaces a **generic** message, e.g. in onboarding hospital profile: *“Failed to load departments (timeout or network error). Please retry.”* (`hospital-profile-step.component.ts`), which is misleading for **401** and for **session termination** after auth flows.
- **`POST /identity/refresh`** can fail with **503**, **500**, **401**, **403**, or **network/timeout** — these are **non-recoverable** for “stay logged in” semantics: user must re-authenticate.
- **`auth.interceptor.ts`** uses **`try/catch` around `refreshToken()`** — refresh returns an **Observable**; **HTTP failures** are **async** and do **not** reliably hit **`catch`**; users may see **silent failures**, **no redirect**, or **inconsistent state**.
- **Concurrent 401s** can trigger **multiple parallel refresh** calls unless a **single-flight** mechanism exists.
- **No queue**: if refresh is in-flight, duplicate refresh attempts can **race** or **loop**.

### Requirements alignment (summary)

| Theme | Expectation |
|-------|-------------|
| **Observable error handling** | **`catchError`** on the **refresh** stream; **no** relying solely on sync **`try/catch`** for async failures. |
| **All refresh failures** | Treat **401, 403, 5xx (incl. 503), status 0 / network / timeout** as **session invalid** → **single logout path**. |
| **Centralized logout** | Clear tokens, org/hospital context, **lookup caches**; **one** navigation to login; **no** duplicate retries. |
| **Anti-loop** | Failed refresh must **not** re-enter refresh logic indefinitely; **`/identity/refresh`** failure must **not** trigger another refresh. |
| **Single refresh + queue** | **One** in-flight refresh; **other** requests **wait** on the same `Observable` and **retry** with new token or **fail together** with logout. |
| **UX** | Optional **“Session expired…”** toast; **no** misleading feature errors for auth/session outcomes. |
| **Logging** | Log refresh failures with **status**, **URL/endpoint**, optional **trace** for ops. |

### Current architecture (relevant)

| Area | Finding |
|------|---------|
| **Interceptor order** (`app.config.ts`) | `[loadingInterceptor, authInterceptor, apiResponseInterceptor]` — document **error** propagation; **401** hits **apiResponse** `catchError` first, then **auth** (rethrow path). |
| **`authInterceptor`** | On **401** only (not **403**): calls **`refreshToken().pipe(switchMap → retry))`**; **`try/catch`** only catches **sync** throw from **`refreshToken()`** when no refresh token (`throw new Error`) — **does not** catch **503** from HTTP. |
| **`AuthService.refreshToken()`** | `post('/identity/refresh', { refreshToken })` — any failure → **Observable error** → must use **`catchError`**. |
| **`apiResponseInterceptor`** | Unwraps success envelopes; logs all API errors — tune **401** / session-related noise. |
| **`AuthService.logout()`** | Clears `SessionService`, `OrganizationService`, `ComplianceContextService` — extend with **lookup cache** clear. |
| **`authGuard`** | Client JWT validity — server may still return **401**; global handler is still required. |

### RxJS patterns (recommended)

1. **`TokenRefreshService` (or `AuthRefreshCoordinator`)**  
   - Exposes **`refreshAccessToken$(): Observable<void>`** (or `LoginResponse`) that:
     - Uses **single-flight**: `shareReplay({ bufferSize: 1, refCount: true })` or **`defer` + `exhaustMap`** / **`switchMap`** from a **mutex** so **only one** HTTP refresh runs.
     - **`refreshToken()`** piped with **`catchError((err) => { logRefreshFailure(err); sessionExpiryCoordinator.handleRefreshFailure(err); return throwError(() => err); })`**.
   - **All** failure types from the **refresh** HTTP call flow through **`catchError`** (not `try/catch`).

2. **Interceptor**  
   - On **401** for **non-auth** requests: **`return tokenRefresh.refreshAccessToken$().pipe(switchMap(() => next(retryReq)))`** — concurrent callers **subscribe to the same** in-flight refresh.  
   - **No** `try/catch` around **`refreshToken()`**; optional **sync** path: if no refresh token, **`return throwError(() => error)`** after coordinator **`no_refresh`** (or call coordinator inside **`defer`**).

3. **Infinite loop prevention**  
   - Requests to **`/identity/login`** and **`/identity/refresh`**: **never** trigger refresh-on-401 **retry** (already partially `isAuthEndpoint`). **If refresh fails** with **401**, interceptor must **not** call **`refreshAccessToken$()`** again — **only** logout.  
   - **Mark** “refresh failed” in coordinator so **subsequent** 401s on the same tick **do not** start new refresh (session already cleared).

4. **Queue semantics**  
   - **Single-flight** `Observable` **is** the queue: **N** concurrent `next()` calls each **await** the same **`refreshAccessToken$()`**; on success **all** retry with new token; on failure **all** receive **`catchError`** once.  
   - **Cancel pending retries**: after logout, **`throwError`** on the **refresh** stream ensures **downstream** `switchMap` does not run **retry** with stale tokens.

### Scope for local refactors

- **`CommonLookupService` / hospital profile**: **do not** show generic network/timeout copy for **401/403** when session is ending; optionally suppress when **`sessionExpired`** query or **coordinator** flag (optional).  
- **`onboarding.guard.ts`**: **401/403** on org-detail → **login** `UrlTree`, not misleading onboarding.

### Non-goals

- Changing backend **401** vs **403** semantics for **resource** APIs (only **refresh** failure handling is unified).  
- Changing **API envelope** shape (`success`, `data`, …).

### must_haves (for PLAN / verification)

1. Refresh **HTTP** failures handled via **`catchError`** on the **refresh** pipeline.  
2. **Any** refresh failure (incl. **503**, network) → **single** idempotent logout + redirect.  
3. **Single-flight** refresh; concurrent **401** → **one** refresh call.  
4. **No** infinite refresh loop on **`/identity/refresh`** failure.  
5. Structured **logging** for refresh failures (status + endpoint).  
6. **No** misleading “Failed to load…” for session/auth outcomes where global handler applies.
