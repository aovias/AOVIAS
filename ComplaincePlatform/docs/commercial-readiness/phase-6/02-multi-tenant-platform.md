# 02 — Multi-Tenant Platform

**Phase:** 6.2  
**Sprint:** 2  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Request isolation | `TenantIsolationMiddleware` after `UseAuthentication` |
| Conflict detection | Query / header / route org & hospital ids vs JWT claims |
| Tenant APIs | `TenantController` secured with `[Authorize]` + `OrgAccess` |
| OrgAccess route keys | Handler accepts `orgId`, `tenantId`, `organizationId` |
| AI cache scoping | `AIOrchestrationService` cache key includes `HospitalId` |
| Notification scoping | `AceNotificationBridge` carries `hospitalId` |
| Branding runtime | WebApp applies org colors to CSS variables on shell load |
| Tests | `Gateway.UnitTests/TenantIsolationEvaluatorTests` |

## Behaviour

1. Authenticated non–`SUPER_ADMIN` requests are inspected for conflicting tenant identifiers.
2. Conflicts return HTTP 403 via `ApiResponseFactory` (`ApiResponseCode.FORBIDDEN`).
3. Setup endpoints (`/api/v*/setup*`) are exempt so first-run wizard can run anonymously.
4. `SUPER_ADMIN` may cross tenants (platform operations).

## Isolation expectations (AI / notifications / storage)

- **AI:** In-memory response cache keys must include hospital id (`ai:{hospitalId}:…`). Never cache by prompt alone.
- **Notifications:** Event payloads must include `hospitalId` so CommunicationService can scope delivery.
- **Storage:** Blob/object prefixes and local paths should be organization/hospital scoped (existing evidence storage patterns); do not share unscoped temp keys across tenants.

## Exit

A caller cannot read another tenant’s resources by forging `organizationId` / `hospitalId` on the query string, headers, or route when JWT claims disagree.
