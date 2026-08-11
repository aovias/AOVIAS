# CRP Phase 3 — Welcome Communication Templates

## Templates

`IdentityEmailTemplates` (Shared.Infrastructure):

1. **WelcomeWithTrial** — trial days, seats, expiry, login CTA, docs + support links, next-steps checklist
2. **GettingStarted** (optional follow-up) — operational checklist + continue CTA

## Config

`OnboardingExperience` section (`OnboardingExperienceOptions`):

- `SupportEmail`
- `DocsBaseUrl`
- `SendGettingStartedEmail` (default true)

Login URL from `Identity:PublicBaseUrl` / Gateway public base via `SignupCommandHandler.BuildLoginUrl()`.

## Send site

Same Phase 2 signup call site after successful commercial provision. GettingStarted is best-effort; **no** change to license/org create order and **no** rollback if email fails.

## Channel

Gateway `IEmailService` (`Console` / `Smtp`).
