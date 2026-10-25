# CRP Phase 3 — Telemetry & Analytics Specification

## Store

OrganizationService entity `OnboardingTelemetryEvent`:

- `OrganizationId`, `UserId`, `EventType`, `Stage`, `CorrelationId`, `OccurredAtUtc`, `PayloadJson`

Table: `OnboardingTelemetryEvents` (migration `20260731180000_Phase3_OnboardingTelemetry`).

## APIs

| API | Auth |
|-----|------|
| `POST /internal/onboarding/events` | Internal |
| `POST /api/v1/onboarding/events` | Tenant JWT (Gateway proxy + structured logs) |

Service: `OnboardingTelemetryAppService.RecordAsync`.

## Event catalog

`SignupCompleted`, `ProvisioningCompleted` (Phase 2 logs), `FirstLogin`, `OnboardingOpened`, `StageEntered`, `StageCompleted`, `StepSkipped`, `FirstUserInvited`, `FirstAssessment`, `OnboardingCompleted`, `OnboardingAbandoned`.

## Client

`OnboardingTelemetryService` emits from `ai-setup-assistant.component.ts` on open, step transitions, skip, assessment/team milestones, and onboard success.
