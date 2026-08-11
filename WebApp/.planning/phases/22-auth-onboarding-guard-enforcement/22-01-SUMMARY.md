---
phase: 22
plan: 22-01
status: complete
---

# Summary 22-01: Global auth + onboarding guard enforcement

## What changed

- Hardened `authGuard` to validate full session/token state using `SessionService.isSessionValid()` and return `UrlTree` redirects.
- Added new `onboardingGuard` to protect `/app/**` routes by organization onboarding status:
  - loads organization detail through `OrganizationService`
  - redirects to `/onboarding` when no hospital is onboarded
- Added `onboardingEntryGuard` for `/onboarding` route:
  - allows non-onboarded users into onboarding
  - redirects onboarded users to `/app/dashboard`
- Applied global guard chain in routing:
  - `/app` → `[authGuard, onboardingGuard]`
  - `/onboarding` → `[authGuard, onboardingEntryGuard]`
- Enhanced `OrganizationService` caching with in-flight request sharing to avoid duplicate API calls during guard checks.
- Updated logout to also clear `ComplianceContextService`, ensuring clean post-logout guard evaluation.

## Files modified

- `src/app/core/guards/auth.guard.ts`
- `src/app/core/guards/onboarding.guard.ts`
- `src/app/core/services/organization.service.ts`
- `src/app/app.routes.ts`
- `src/app/core/auth/auth.service.ts`

## Verification notes

- No linter errors in modified files.
- Guard matrix now enforces:
  - unauthenticated → login
  - authenticated + not onboarded → onboarding only
  - authenticated + onboarded → app routes
- Guard decisions are resilient on deep links and refresh because routing checks run before feature entry.

