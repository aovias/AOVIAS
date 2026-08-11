# Phase 26 — Verification

**Date:** 2026-04-03  
**Status:** passed (build + code review)

## must_haves

| Criterion | Evidence |
|-----------|----------|
| Single wizard store | `AiOnboardingWizardStore` + `ai_onboarding_wizard_v1` |
| POST `/onboarding/onboard` | `OnboardingService.submitOnboard`, `OnboardSubmitRequest` |
| Team departments from profile | `allowedDepartments` + `TeamMember.departmentId` |
| Review + submit | `CompletionStepComponent` + `submitOnboard()` |
| Validation | `AiOnboardingValidationService` + step handlers |
| Build | `ng build --configuration=development` OK |

## Manual QA

- Refresh mid-wizard: state from `sessionStorage`.
- Submit failure: toast + `submitError` + retry without clearing wizard (until success clears session).

## Payload contract

See **`POST_ONBOARD_PAYLOAD.md`** for backend.
