# CRP Phase 3 — Operational Runbook

## Deploy

1. Apply OrganizationService migration `Phase3_OnboardingTelemetry`.
2. Deploy Gateway (journey + events endpoints, `OnboardingExperience` config).
3. Deploy WebApp (welcome, trial strip, stepper, telemetry emit).
4. Restart Gateway / OrganizationService if DLL locks blocked prior builds.

## Config checklist

```json
"OnboardingExperience": {
  "SupportEmail": "support@example.com",
  "DocsBaseUrl": "https://docs.example.com",
  "SendGettingStartedEmail": true
}
```

Ensure Identity/Gateway public base URL is set so welcome email login links resolve.

## Verify smoke

1. Signup → welcome email (Console or SMTP) includes login/docs/support.
2. `GET /api/v1/onboarding/journey` returns stages + trial.
3. Wizard shows trial strip and progress %.
4. `POST /api/v1/onboarding/events` persists rows in `OnboardingTelemetryEvents`.
5. Skip stays on `/onboarding` when not onboarded.

## Rollback

Feature is additive. Disable GettingStarted via config; journey/telemetry unused if clients are not deployed. Do not roll back Phase 2 license tables.
