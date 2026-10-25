# 01 — Licensing Platform

**Phase:** 6.1  
**Sprint:** 1  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| License keys | `LicenseKey` entity + `LicenseAppService.IssueAsync` |
| Activation | Online + offline signed payload |
| Heartbeat | `LicenseHeartbeat` + `POST /licenses/heartbeat` |
| Seats | `LicenseSeatAssignment` + MaxSeats enforcement |
| Grace / expiry | Status refresh → Active / Grace / Expired |
| Entitlements | JSON feature codes; `entitlements/check` |
| Gateway | `LicensesController` proxies `/internal/licenses/*` |
| Soft gate | `LicenseEntitlementMiddleware` (header `X-Require-Entitlement`) |

## APIs (Gateway)

- `POST /api/v1/licenses/issue`
- `POST /api/v1/licenses/activate`
- `GET /api/v1/licenses/status?organizationId=`
- `POST /api/v1/licenses/heartbeat`
- `POST /api/v1/licenses/seats/assign`
- `GET /api/v1/licenses/entitlements/check`
- `GET /api/v1/licenses/activations`
- `GET /api/v1/licenses/usage`

## Migration

`20260726210000_Phase6_LicensingPlatform`

## Exit

Customer can issue/activate a key, see seats/usage, and entitlement checks return operational status including grace messaging.
