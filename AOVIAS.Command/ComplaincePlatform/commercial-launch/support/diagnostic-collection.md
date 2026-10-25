# Diagnostic Collection

## Primary pack (preferred)

Authenticated platform / org admin:

```http
GET /api/v1/commercial/diagnostics?organizationId=<guid>
Authorization: Bearer <admin-token>
```

**Controller:** `Gateway/Controllers/CommercialOpsController` (Phase 6 commercial ops).  
**UI:** Settings → Platform → Commercial Ops → download diagnostics.

Returns a **redacted** JSON pack (versions, masked license key, config flags without secrets, backup presence indicators). Attach to the support ticket.

## Companion health

```http
GET /api/v1/commercial/customer-health?organizationId=<guid>
GET /api/v1/licenses/status?organizationId=<guid>
GET /api/v1/licenses/usage?organizationId=<guid>
```

## Always capture

| Item | Why |
|------|-----|
| `X-Correlation-ID` / `X-Correlation-Id` | Trace across Gateway + services |
| VERSION (`1.0.0` or `/VERSION` file) | Release match |
| UTC timestamps | Log alignment |
| Browser + OS | WebApp issues |
| Recent change | Upgrade, restore, maintenance.lock |

## Ops-only extras (do not email secrets)

- Gateway / service logs around Correlation ID  
- `scripts/ops/validate-production.ps1` output  
- Hangfire / outbox poison admin notes if message processing stuck  

## Privacy

- Strip PHI from screenshots.  
- Diagnostics are designed to redact secrets — still treat as confidential.  
- Do not paste full connection strings into tickets.
