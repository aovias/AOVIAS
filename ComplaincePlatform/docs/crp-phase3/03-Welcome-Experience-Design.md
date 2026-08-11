# CRP Phase 3 — Welcome Experience Design

## Surface

`welcome-step.component.ts` inside the existing AI setup assistant wizard (step 1).

## Content

- Organization name, admin name/email
- Trial block (days remaining, expiry) from journey API with Phase 2 sessionStorage fallback
- Progress %, next recommended action, ETA minutes
- Support / docs links from journey quick actions / config
- Primary CTA: Start / Resume setup
- Coach tip (dismissible via `onboarding.tour.dismissed`)

## Skip / Resume later

**Skip for now** must:

1. Emit `StepSkipped` / resume-later telemetry
2. Stay on `/onboarding` with a clear message
3. **Not** navigate to `/app/dashboard` while `!IsOnboarded`

## Data loading

1. Prefer `GET /api/v1/onboarding/journey`
2. On failure, show retry banner and fall back to signup trial snapshot (`getTrialProvisioningStorageKey`)
3. Role guidance map adjusts subtitle / next-action copy
