# CRP Phase 1 — Configuration Guide

## IdentityService

### Email verification

```json
"Identity": {
  "EmailVerificationMode": "Async",
  "PublicBaseUrl": "https://app.example.com",
  "EmailVerification": {
    "TokenLifetimeHours": 48,
    "ReminderAfterDays": 3,
    "ResendCooldownoldownSeconds": 60
  },
  "RelaxLoginSecurityGates": false,
  "EnableBackgroundJobs": true
}
```

| Mode | Behavior |
|------|----------|
| `None` | No verify email; confirmed on create |
| `Async` | Default SaaS; login allowed; verify email sent |
| `RequiredBeforeLogin` | Login blocked until verified |
| `RequiredBeforeOrganizationProvisioning` | Anonymous org signup blocked until verified identity |

Legacy: `RequireEmailVerification=true` → `RequiredBeforeLogin`.

### Email transport

```json
"Email": { "Provider": "Smtp" },
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "UserName": "...",
  "Password": "...",
  "FromAddress": "noreply@example.com",
  "FromDisplayName": "AOVIAS Command",
  "EnableSsl": true,
  "MaxRetryAttempts": 3
}
```

Development: `Email:Provider=Console` (default).

### Gateway

Mirror `Identity:EmailVerificationMode` so SignupCommand applies the org-provisioning gate.

### Environments

| Env | Email Provider | Relax gates | Mode |
|-----|----------------|-------------|------|
| Development | Console | may be true | Async |
| QA / Staging | Smtp | false | Async |
| Production | Smtp | **false** | Async (or stricter) |
