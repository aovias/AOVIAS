# Contact Form — Email Delivery (Hostinger SMTP)

## Flow

```
Visitor → validated form → POST /api/contact → EmailService → EmailProvider
                                                      ├─ Email 1: internal notification → CONTACT_FORM_TO
                                                      └─ Email 2: confirmation → visitor email
```

Credentials never ship to the browser (`NEXT_PUBLIC_*` must not contain SMTP secrets).

## Architecture

| Layer | Role |
| --- | --- |
| `EmailService` (`lib/email/email-service.ts`) | Retries, logging, provider-agnostic send |
| `EmailProvider` | Transport adapter (`smtp` \| `resend` \| `brevo` \| `console`) |
| `deliverContactEmails` | Builds both messages; Contact API stays provider-neutral |
| `config/email/hostinger.settings.json` | Non-secret Hostinger host/ports/retry defaults |

Switching later: set `EMAIL_PROVIDER=resend` + `RESEND_API_KEY`. No Contact API rewrite.

## Environment (server-only)

```bash
EMAIL_PROVIDER=smtp
SMTP_HOST=smtp.hostinger.com          # optional — defaults from hostinger.settings.json
SMTP_PORT=587                         # or 465 with SMTP_SECURE=true
SMTP_SECURE=false
SMTP_USER=admin@aovias.com
SMTP_PASSWORD=********
EMAIL_FROM="AOVIAS Website <admin@aovias.com>"
CONTACT_FORM_TO=admin@aovias.com
```

`SMTP_PASS` is accepted as a legacy alias for `SMTP_PASSWORD`.

## Hostinger settings JSON

`apps/website/src/config/email/hostinger.settings.json` stores recommended Hostinger Mail configuration (host, ports, timeouts, retry). **Do not put passwords in this file.**

## Failure handling

- Transient SMTP failures: exponential backoff retries (see settings JSON).
- Internal notification failure → `502` + user-friendly message (retryable).
- Confirmation failure after successful notification → still `200` (enquiry recorded); confirmation error is logged.
- Duplicate identical submissions within 2 minutes are acknowledged without re-sending.

## Form UX

- Submit button disabled while sending (`FormEngine` + `submittingRef` guard).
- Success replaces the form (prevents accidental double submit).
