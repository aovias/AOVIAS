# 07 — Commercial Operations

**Phase:** 6.7  
**Sprint:** 5  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Trial provision | `POST /api/v1/commercial/provision` (`CommercialOpsController`) |
| Customer health | `GET /api/v1/commercial/customer-health?organizationId=` |
| Diagnostics pack | `GET /api/v1/commercial/diagnostics?organizationId=` |
| WebApp admin UI | Settings → Platform → Commercial Ops |

## Provision trial

Issues a **Trial** license via existing OrganizationService internal API (`POST /internal/licenses/issue`) and returns:

- License key code / id / expiry / seats
- Checklist: activate → assign seats → onboard wizard → entitlements → heartbeat → health

### Example body

```json
{
  "organizationId": "00000000-0000-0000-0000-000000000001",
  "customerName": "Demo Hospital Group",
  "maxSeats": 10,
  "validDays": 30,
  "gracePeriodDays": 7
}
```

Requires authenticated user with `AdminOnly` policy (`SUPER_ADMIN`, `ORG_ADMIN`, or `SYSTEM`).

## Customer health

Aggregates license status from OrganizationService plus readiness stubs (onboarding, identity, compliance, backup manifest).

## Diagnostics pack

Downloads a redacted JSON file (versions, license with masked key, config flags without secrets, backup presence).

## Related

- Licensing APIs: `01-licensing-platform.md`
- WebApp: `/settings/platform/commercial`

## Exit

Platform admin can provision a trial, inspect customer health, and download a diagnostics pack without leaving the Gateway façade.
