# CRP Phase 7 — Invitation Management

APIs: create, bulk, list, resend, cancel, revoke, redeem/preview, redeem.

Statuses: Pending, Accepted, Expired, Revoked, Cancelled.

Token: raw in email URL only; SHA-256 hash stored. Expiry configurable (`WorkforceOnboarding:InvitationExpiryDays`).
