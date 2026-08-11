# Plan 26-01 — Summary

## Status: Complete

## Delivered

- **`OnboardSubmitRequest`** + **`TeamInviteRow`** (`onboard-submit-request.model.ts`); **`buildOnboardSubmitRequest`** (`onboard-submit.mapper.ts`).
- **`AiOnboardingWizardStore`**: signals + **`sessionStorage`** key **`ai_onboarding_wizard_v1`**; mirrors **`OnboardingStore.setFrameworkCodes`** on framework updates and on init restore; **`clearWizardSession`** after successful submit.
- **`AiOnboardingValidationService`**: hospital, frameworks, assessment, team (unique emails + department IDs ⊆ profile).
- **`OnboardingService.submitOnboard`** → **`POST /onboarding/onboard`**.
- **`AiSetupAssistantComponent`**: uses wizard store; validates each step; **`allowedDepartments`** computed from cached departments + profile IDs; completion **`submitOnboard`** with loading/error/retry.
- **`TeamInvitationStepComponent`**: **`allowedDepartments`** input, department **`<select>`**, duplicate-email message, **`departmentId`** on **`TeamMember`**.
- **`CompletionStepComponent`**: review UI + submit button; **`CompletionStepSummary`** model.
- **Payload doc for backend:** `POST_ONBOARD_PAYLOAD.md`.

## Build

- `ng build --configuration=development` — passed.
