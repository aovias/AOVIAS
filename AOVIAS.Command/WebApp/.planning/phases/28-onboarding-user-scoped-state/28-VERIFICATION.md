# Phase 28 — Verification

**Status:** passed (code review + `ng build`)

## must_haves (28-01-PLAN)

| Criterion | Evidence |
|-----------|----------|
| User-scoped `sessionStorage` keys | `onboarding-storage.keys.ts` + both stores |
| Logout clears onboarding | `AuthService.logout` → `logoutCleanup()` before `clearSession()` |
| Login user change | `LoginComponent` + `handleLoginIdentity(prior, new)` |
| Load only for current user | Keys include `userId`; no `userId` → AI wizard defaults only |
| Missing user id | AI wizard does not persist; signup uses `anonymous` bucket |
| Refresh token user change | `AuthService.refreshToken` tap |

## Follow-ups (deferred)

- **Multi-tab:** `sessionStorage` does not sync across tabs via `storage` event; same-origin logout in another tab may leave this tab’s memory stale until navigation — acceptable for v1 or add `localStorage` logout broadcast later.
- **Backend:** `POST /onboarding/onboard` should validate org/hospital ownership server-side.
