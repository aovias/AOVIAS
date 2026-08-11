# CRP Phase 1 — Identity Architecture Review

**Date:** 2026-07-31  
**Status:** Completed (hardening; no redesign)  
**Grounding:** Phase 0 Architecture Spec + Discovery v1.1

## Summary

Phase 1 extended the existing ASP.NET Identity + custom JWT stack. No parallel authentication model was introduced. Duende remains secondary.

## Components reviewed

| Area | Finding | Action |
|------|---------|--------|
| Registration | Self-register + internal register + Gateway signup | Mode-aware verification; email normalize |
| Email verification | Tokens hashed; verification off by default | `EmailVerificationMode`; resend; reminders |
| Authentication | Login/MFA/refresh solid | Mode-based unconfirmed login gate |
| Password | History, reset, force-change, max-age | HTML reset template; max-age already gated |
| Sessions | Family rotation, revoke, cleanup | Validated; admin revoke APIs retained |
| Email | Console only | `SmtpEmailService` + provider switch |
| Security | Lockout 5/15, LoginPolicy rate limit | Production relax gates disabled |

## Ownership unchanged

Identity owns credentials, verification, sessions. Gateway orchestrates signup. Communication delivers (shared SMTP). Organization remains tenant (out of Phase 1 mutation scope except signup verification gate).
