---
phase: 22
status: passed
---

# Verification Phase 22: Auth + onboarding guard enforcement

## Must-haves

- [x] Protected routes globally enforce authentication via `authGuard`
- [x] Protected app routes enforce onboarding status via `onboardingGuard`
- [x] Non-onboarded logged-in users are redirected to `/onboarding`
- [x] Onboarded logged-in users can access `/app/**`
- [x] `/onboarding` route is restricted to non-onboarded users (`onboardingEntryGuard`)
- [x] Organization detail is centrally reused and de-duplicated with in-flight caching
- [x] Logout clears auth/session + organization + compliance context state

## Edge-case coverage

- Token missing/expired: redirected to `/login` with `returnUrl`
- Organization API failure in onboarding guard: safe redirect to `/onboarding`
- Empty/no hospitals: treated as non-onboarded
- Deep link/refresh: guard chain re-evaluates before feature render

## Quality checks

- No direct HttpClient usage added in components/guards
- No linter errors in touched files

