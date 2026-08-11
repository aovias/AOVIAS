# Plan 28-01 — Summary

## Completed

- **`onboarding-storage.keys.ts`**: `getAiWizardStorageKey`, `getSignupOnboardingStorageKey` (anonymous bucket for public signup), legacy key constants.
- **`OnboardingStateIsolationService`**: legacy + anonymous purge, `purgeStorageForUser`, `logoutCleanup`, `handleLoginIdentity` (prior vs new user), `resetInMemoryStores`.
- **`AiOnboardingWizardStore`**: persists under `ai_onboarding_wizard_v1:<userId>`, migrates legacy unscoped key, no persist without `userId`, `reinitializeForCurrentUser`.
- **`OnboardingStore`**: persists under `onboarding_state:<userId>` or `onboarding_state:anonymous`, migrates legacy `onboarding_state`, `reinitializeFromSession`.
- **`AuthService.logout`**: `logoutCleanup()` before `clearSession()`.
- **`AuthService.refreshToken`**: `handleLoginIdentity` when `userId` changes.
- **`LoginComponent`**: captures `priorUserId` before `login()`, calls `handleLoginIdentity` after auth (tap) updates session.

## Verification

- `npx ng build --configuration=development` — **passed**.
