# CRP Phase 2 — Seat Assignment Strategy

## Signup admin seat

After successful issue (and activate when required), Gateway calls:

`POST /internal/licenses/seats/assign` with admin `userId` + email.

## Rules

- Requires operational license (`Active` / `Grace` / `Issued`).
- Same user already seated → return existing (idempotent).
- Seat limit enforced via `UsedSeats` / `MaxSeats`.
- Seat failure after retry → fail signup (no second license issued).

## Rollback

`POST /internal/licenses/seats/release` deactivates active seats for the user and decrements `UsedSeats`.

## Future

Enterprise floating seats are out of scope; seat model remains named assignment on the LicenseKey.
