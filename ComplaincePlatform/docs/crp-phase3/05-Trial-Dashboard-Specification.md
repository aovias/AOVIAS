# CRP Phase 3 — Trial Dashboard Specification

## Component

Compact `trial-status-panel` on the onboarding layout (all wizard steps).

## Fields

- Days remaining, expiry UTC
- License status / type
- Seats used / max
- Entitlement chips
- Upgrade CTA labeled for later phases (**non-functional**; no Stripe)

## Data source

Refresh from `GET /onboarding/journey` trial summary (compose of existing license status/usage). Do not rely solely on signup sessionStorage.

## Storage hygiene

Trial snapshot key is user-scoped via `getTrialProvisioningStorageKey()`. Stale global `aovias.signup.trial` is cleared into user-scoped storage and purged on logout by `OnboardingStateIsolationService`.

## Pattern reuse

Status shaping aligns with `licensing-page.component.ts`.
