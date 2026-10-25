# License Renewal Process (Manual Keys)

**Product:** AOVIAS Command v1.0.0  
**Payments:** Not in product — commercial ops issues new keys manually

## Overview

```
CS / Sales agree renewal → Admin issues new key → Customer activates → Heartbeat / seats verified
```

No in-app payment, invoice scrape, or auto-charge.

## Timeline

| Window | Action |
|--------|--------|
| T−60 days | CS renewal conversation; confirm seats / entitlements |
| T−30 days | Quote / PO (offline commercial process) |
| T−14 days | Issue renewal key (same or new `organizationId` binding) |
| T−7 days | Customer activates; verify status |
| Expiry | If late: **Grace** period (per key `gracePeriodDays`) keeps operational messaging |
| Post-grace | Status **Expired** — features may soft-fail; escalate urgently |

## Issue renewal key

Platform / commercial admin:

```http
POST /api/v1/licenses/issue
```

Or trial-style helper for short extensions:

```http
POST /api/v1/commercial/provision
```

Recommended fields: `organizationId`, `maxSeats`, `validDays`, `gracePeriodDays`, entitlement JSON, `customerName`.

Record key code in CS CRM (not in public tickets). Share securely with hospital ORG_ADMIN.

## Customer activates

Follow [activation-guide.md](activation-guide.md) (online or offline). Prefer same `machineFingerprint` as prior production host unless migrating.

```http
GET /api/v1/licenses/status?organizationId=
GET /api/v1/licenses/usage?organizationId=
```

## Seat / SKU changes

| Change | Process |
|--------|---------|
| More seats | Issue key with higher MaxSeats or update per internal license APIs; re-assign seats |
| Add AI / ACE | Include `ai.assist` / `ace.insights` in entitlements on new key |
| Downgrade | Commercial approval; new key with reduced entitlements; communicate feature loss |

## Failure modes

| Issue | Response |
|-------|----------|
| Customer lost key | Re-issue; revoke old if compromised |
| Activated on wrong org | Do not transfer ad hoc — open Sev-2 commercial ticket |
| Grace ending | Daily CS chase; temporary short `validDays` extension key if PO pending |
| Payment dispute | Hold new key until Finance clears — product has no payment state |

## CS checklist

- [ ] Renewal opportunity logged  
- [ ] New key issued and vaulted  
- [ ] Customer activated  
- [ ] Health check green: `GET /api/v1/commercial/customer-health?organizationId=`  
- [ ] Next QBR date set  

## Related

- [portal-launch-checklist.md](portal-launch-checklist.md)  
- Customer Success: [`../customer-success/renewal-strategy.md`](../customer-success/renewal-strategy.md)
