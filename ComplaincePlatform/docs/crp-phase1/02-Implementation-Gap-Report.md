# CRP Phase 1 — Implementation Gap Report

| Gap (pre-Phase 1) | Resolution |
|-------------------|------------|
| `RequireEmailVerification` boolean only | Replaced by `Identity:EmailVerificationMode` with legacy bool mapping |
| Console email only | `Email:Provider=Smtp\|Console` + `SmtpEmailService` |
| No HTML templates | `IdentityEmailTemplates` (verify, welcome, reset, security, invitation) |
| No resend verification | `POST /api/v1/auth/resend-verification` (+ Gateway proxy) |
| Register UI stub | Production `/register` → `/identity/self-register` |
| Gateway `identity/register` = org signup only | Added `self-register` for identity-only path |
| Expired/used token messaging | Distinct audit + API messages |
| Verification reminders | `EmailVerificationReminderHostedService` |
| Communication delivery retry counter unused | Increments `RetryCount` on failed send |
| RequiredBeforeOrganizationProvisioning | SignupCommand blocks anonymous provision |
| Production RelaxLoginSecurityGates | Explicit `false` in Production appsettings |

## Remaining (acceptable / later CRP)

- Full invitation token redeem UX (Phase 0 noted; not Phase 1 DoD for licensing)
- Real SMTP host credentials must be supplied per environment (config only)
- Running Identity/Gateway processes may need restart to load new binaries (file locks during build)
