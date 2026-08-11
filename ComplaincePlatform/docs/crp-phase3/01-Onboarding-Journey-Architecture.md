# CRP Phase 3 — Onboarding Journey Architecture

## Purpose

Composed, read-only journey API that surfaces org, hospital, license/trial, and stage progress for the existing AI onboarding wizard—without changing Phase 2 provisioning or LicenseKey SSOT.

## Boundary

| In scope | Out of scope |
|----------|--------------|
| `GET /api/v1/onboarding/journey` | Billing / Stripe |
| Stage status composition | Second wizard |
| Trial summary from license proxies | Identity redesign |
| Quick actions for Continue / Invite / Docs | Changing `SignupCommandHandler` license order |

## Components

| Layer | Artifact |
|-------|----------|
| DTO | `Gateway.DTOs.OnboardingJourneyDto` (+ stages, trial, quick actions) |
| Pure composer | `OnboardingJourneyComposer` |
| Query | `GetOnboardingJourneyQuery` / `GetOnboardingJourneyQueryHandler` |
| HTTP | `OnboardingController` → `GET journey` |
| Client | `OnboardingJourneyService` |

## Composition inputs

- Organization detail (name, hospital, `IsOnboarded`)
- License status/usage (via existing LicensesController proxies)
- Role from JWT claims
- Optional wizard signals (frameworks / assessment / team) when available

Composer **does not write** business state.

## Stage codes

`Welcome`, `EmailVerified`, `OrganizationReady`, `LicenseActive`, `HospitalProfile`, `FrameworkSelection`, `ComplianceAssessment`, `TeamSetup` (optional), `AiRecommendations`, `OperationalReadiness`.

Statuses: `Completed` | `Current` | `Pending` | `Blocked` | `Optional`.

## Frozen

Phase 2 Signup trial issue/activate/seat and Identity auth remain unchanged.
