# Organization member invitations (email-ready foundation)

This document describes **Phase 23-06** storage and API direction. The **wizard default** remains immediate Identity register + org member create (generated password) until email delivery exists.

## Data model (`OrganizationMemberInvitations`)

| Column | Purpose |
|--------|---------|
| `OrganizationId`, `HospitalId`, `DepartmentId?`, `Role` | Where the invite applies. |
| `Email`, `NormalizedEmail` | Contact; `NormalizedEmail` is upper-invariant for lookups. |
| `TokenHash` | Hash of opaque token (e.g. SHA-256 hex) when email flow is implemented. |
| `ExpiresAt`, `Status` | `InvitationStatus`: Pending, Accepted, Expired, Revoked. |
| `AcceptedAt`, `AcceptedUserId` | Set when invite is redeemed. |

## Duplicate policy

- **Filtered unique index** on `(OrganizationId, NormalizedEmail)` **where `Status = Pending`**, so only one active pending invite per org per email; new invites after accept/revoke are allowed.

## Future API (sketch)

- `POST /internal/invitations` — create pending row, enqueue email (not implemented).
- Wizard with `UseEmailInvites: true` would call this instead of `POST /internal/users/register` for each row.

## Feature flag (Gateway)

- `OnboardingWizard:UseEmailInvites` in `appsettings.json` (default `false`).
- When `true`, the handler logs that the **email pipeline is not yet implemented** and **still** uses the legacy register path until the next phase wires persistence + mailer.

## Related

- `docs/onboarding-wizard-rules.md` — product rules and auth.
