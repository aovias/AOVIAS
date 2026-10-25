# CRP Phase 6 — License Domain Model

## LicenseType

Trial, Subscription, Perpetual, Offline, Enterprise, Evaluation, Developer.

## LicenseStatus

Issued → Active (alias Operational / Activated) → Grace → Expired; plus Suspended, Revoked, Archived.

## Ownership

License owns status, expiration, activation, seats, entitlements, renewal/suspension/revocation semantics.

OrganizationCommercialDisplayStatus is derived: Trial / Operational / ReadOnly / Suspended / Archived.
