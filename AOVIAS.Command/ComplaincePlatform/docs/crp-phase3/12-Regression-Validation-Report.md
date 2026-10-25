# CRP Phase 3 — Regression Validation Report

## Automated

| Suite | Focus | Result |
|-------|--------|--------|
| `OnboardingJourneyComposerTests` (3) | Percent, nextAction, auditor Blocked | Passed |
| `RecordOnboardingTelemetryCommandHandlerTests` (2) | Proxy happy path + org required | Passed |
| `SignupCommandHandlerTrialProvisioningTests` (3) | Phase 2 trial path (+ experience options) | Passed |
| `OnboardingTelemetryAppServiceTests` (2) | Persist + validation | Passed |
| `LicenseAppServiceTests` (9) | License SSOT unchanged | Passed |

## Manual checklist

- [ ] First login after signup lands on onboarding welcome
- [ ] Welcome shows org, trial, progress, ETA
- [ ] Trial strip visible on all steps; seats/status refresh from journey
- [ ] Stepper % / completed click-back / Team optional badge
- [ ] Resume later does not bounce to dashboard while `!IsOnboarded`
- [ ] Assessment helpText visible when API provides it
- [ ] Coach tip dismisses and stays dismissed
- [ ] Role-aware AI copy for non-admin (if invite available)
- [ ] Welcome + GettingStarted appear in email Console
- [ ] Telemetry: OnboardingOpened, Stage*, StepSkipped, OnboardingCompleted

## Definition of done

Guided onboarding; personalized welcome; progress + ETA; trial strip; resume/recovery; enriched welcome email; telemetry funnel; provisioning/identity/licensing cores unchanged; regression green.
