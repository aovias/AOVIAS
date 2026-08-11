# CRP Phase 1 — Security Hardening Report

## Controls validated

| Control | Status |
|---------|--------|
| Password complexity (Identity options) | Enforced |
| Account lockout 5 failures / 15 minutes | Enforced |
| Rate limiting `LoginPolicy` 30/min | Enforced on auth endpoints including resend |
| Refresh token rotation + reuse detection | Existing SessionService |
| Access token JTI revocation | Existing |
| JWT clock skew Zero | Existing |
| MFA TOTP + recovery | Existing |
| Adaptive / risk / trusted devices | Existing (relaxed only when `RelaxLoginSecurityGates`) |
| Production `RelaxLoginSecurityGates` | **false** |
| Production `Email:Provider` | **Smtp** (host must be configured) |
| Sensitive logging | Tokens not written in verification emails to logs as clear tokens beyond SMTP subject |

## Recommendations for operators

1. Set `JwtSettings:SecretKey` via secret store in Production.  
2. Set `Smtp:*` before enabling real traffic.  
3. Prefer `EmailVerificationMode=Async` for SaaS; tighten to `RequiredBeforeLogin` when ready.  
4. Do not enable `RelaxLoginSecurityGates` outside Development.
