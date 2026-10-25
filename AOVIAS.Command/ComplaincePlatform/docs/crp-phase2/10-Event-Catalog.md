# CRP Phase 2 — Event Catalog

All events are emitted as structured Gateway logs (correlation via `HttpContext.TraceIdentifier`).

| Event | When | Payload keys |
|-------|------|--------------|
| OrganizationProvisioned | Org created | organizationId, step |
| HospitalProvisioned | Hospital created | organizationId, step |
| DefaultDataSeeded | Ensure-defaults OK | organizationId, step |
| MembershipCreated | Member linked | organizationId, step |
| TrialIssued | get-or-issue (Issued or Reused) | organizationId, step |
| LicenseActivated | Online activate OK | organizationId, step |
| SeatAssigned | Admin seat OK | organizationId, step |
| WelcomeEmailSent | Email attempted | organizationId, step |
| ProvisioningCompleted | Happy path end | organizationId, step |
| ProvisioningFailed | Exception | correlationId, step |
| ProvisioningRolledBack | After rollback | correlationId, step |

**Idempotency key format:** `{EventType}:{organizationId:N}:{step}`
