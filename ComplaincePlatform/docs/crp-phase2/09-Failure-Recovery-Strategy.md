# CRP Phase 2 — Failure Recovery Strategy

| Failure | Behavior |
|---------|----------|
| Org / hospital / user mid-pipeline | Existing LIFO rollback stack |
| Ensure-defaults fails | Fail signup + rollback prior steps |
| License issue fails (`AutoIssueOnSignup=true`) | Fail signup + rollback |
| Activate fails (AutoActivate=true) | Retry once; then fail + rollback |
| Seat fails | Retry once; fail signup; no second license |
| Welcome email fails | Log + continue (no rollback) |
| Partial re-entry | get-or-issue / seat / defaults idempotent; org name still 409 |
| Duplicate trial via Issue API | 400 validation (`already has an operational`) |

## Observability

Structured logs: `ProvisioningEvent {EventType} … IdempotencyKey={EventType}:{orgId}:{step}`.

Events: OrganizationProvisioned, HospitalProvisioned, MembershipCreated, DefaultDataSeeded, TrialIssued, LicenseActivated, SeatAssigned, WelcomeEmailSent, ProvisioningCompleted / Failed / RolledBack.

## Compensating license actions

- Revoke: `POST /internal/licenses/revoke`  
- Release seat: `POST /internal/licenses/seats/release`
