# CRP Phase 3 — Progress Tracking Specification

## Stepper

`onboarding-stepper.component.ts` shows:

- Completion percent (from journey or local wizard validation)
- Current / pending labels
- Optional badge for Team step
- Click navigation back to completed steps (wizard store index)
- Sticky Continue setup / Resume later in the shell

## Side checklist

Fed by journey `stages[]` plus local validation from `ai-onboarding-validation.service.ts`.

## Persistence

In-progress answers remain in `AiOnboardingWizardStore` (user-scoped). Journey API is refreshable SSOT for commercial/org flags; the wizard draft is SSOT for mid-flow form state.

## Telemetry

Step transitions emit `StageEntered` / `StageCompleted`; abandon/leave may emit `OnboardingAbandoned`.
