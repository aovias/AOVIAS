# CRP Phase 3 — Recovery Strategy

## Persistence

- User-scoped wizard draft (`AiOnboardingWizardStore` + isolation keys)
- Journey API refresh after network errors
- Retry banners on journey / license fetch failure

## Trial key cleanup

- Prefer user-scoped `getTrialProvisioningStorageKey()`
- Migrate/clear stale global `aovias.signup.trial`
- Purge on logout via `OnboardingStateIsolationService`

## Auth expiry

Existing auth redirect with `returnUrl=/onboarding` so users resume the wizard after re-login.

## Skip vs abandon

Resume later stays on onboarding; does not treat incomplete orgs as dashboard-ready.
