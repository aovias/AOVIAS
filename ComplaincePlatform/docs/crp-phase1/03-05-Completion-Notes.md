# CRP Phase 1 — Completion Notes (WS3–5)

## Email verification flow

- Hashed single-use tokens; configurable lifetime  
- Resend with cooldown; already-verified / expired / invalid handling  
- Reminder hosted service  
- Audit event codes: `EMAIL_VERIFICATION_REQUESTED`, `EMAIL_VERIFIED`, `EMAIL_VERIFICATION_FAILED`

## SMTP

- `SmtpEmailService` with TLS + retries  
- Templates in `IdentityEmailTemplates`  
- Shared DI helper `AddConfigurableEmailService`

## Registration

- WebApp `/register` production form  
- Gateway `POST /api/v1/identity/self-register`  
- `/signup` preserved + link to create account
