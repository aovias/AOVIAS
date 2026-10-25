# CRP Phase 1 — Operational Runbook

## Health checks

1. IdentityService `/health` (or existing health endpoint).  
2. Send test verification: register test user → confirm console or SMTP receipt.  
3. `POST /api/v1/identity/resend-verification` with cooldown respected.

## Common incidents

| Symptom | Check |
|---------|-------|
| No emails | `Email:Provider`, Smtp host/credentials, firewall 587/465 |
| Users cannot login | Mode `RequiredBeforeLogin` + unconfirmed email; lockout; ForcePasswordChange |
| Org signup 403 | Mode `RequiredBeforeOrganizationProvisioning` — verify then provision |
| Verification link invalid | Expiry hours; already used; resend |

## Jobs

- `EmailVerificationReminderHostedService` every ~6h when background jobs enabled.  
- `IdentityCleanupHostedService` for sessions/attempts.

## Correlation

Pass `X-Correlation-Id` on Gateway → Identity for audit/log stitching.
