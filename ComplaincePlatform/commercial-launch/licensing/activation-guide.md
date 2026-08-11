# License Activation Guide

**Product:** AOVIAS Command v1.0.0  
**Surfaces:** Gateway `/api/v1/licenses/*`, WebApp **Settings → Platform → Licensing**, Commercial Ops provision

Licensing is issued and activated via existing Phase 6 platform APIs — no separate payment portal.

## Prerequisites

- Organization ID (GUID) matching the issued key
- Key code from CS / `POST /api/v1/commercial/provision` or `POST /api/v1/licenses/issue`
- Authenticated admin (`ORG_ADMIN` / `SUPER_ADMIN` as required)
- Stable **machine fingerprint** (hostname or agreed ID) for the deployment host

## Online activation (default)

Use when Gateway can reach OrganizationService licensing APIs on the customer network.

### UI

1. Sign in as hospital / platform admin.
2. Open **Settings → Platform → Licensing**.
3. Enter key code + confirm organization.
4. Activate (online).
5. Assign seats to users.
6. Confirm status shows **Active** (or **Grace** near expiry).

### API

```http
POST /api/v1/licenses/activate
Content-Type: application/json
Authorization: Bearer <token>

{
  "keyCode": "AOVIAS-XXXX-XXXX",
  "organizationId": "00000000-0000-0000-0000-000000000001",
  "machineFingerprint": "hosp-prod-gw-01",
  "activatedBy": "admin@hospital.example",
  "offline": false
}
```

Then:

```http
GET /api/v1/licenses/status?organizationId=<guid>
POST /api/v1/licenses/heartbeat
```

Heartbeat body includes `organizationId`, `machineFingerprint`, optional `productVersion`, `seatUsage`.

## Offline activation

Use for air-gapped or restricted networks.

1. CS issues a key with offline intent (or customer requests offline mode).
2. Customer supplies `machineFingerprint` and an agreed `offlinePayload` string (installation token / request blob documented in the commercial ticket).
3. Activate with `offline: true`:

```http
POST /api/v1/licenses/activate
Content-Type: application/json

{
  "keyCode": "AOVIAS-XXXX-XXXX",
  "organizationId": "00000000-0000-0000-0000-000000000001",
  "machineFingerprint": "airgap-host-01",
  "offlinePayload": "<customer-provided-payload>",
  "activatedBy": "admin@hospital.example",
  "offline": true
}
```

OrganizationService signs `keyCode|organizationId|machineFingerprint|offlinePayload` and stores activation with `ActivationMode = Offline`. License type is set to Offline.

4. Confirm via `/licenses/status` and `/licenses/activations`.

## Seats & entitlements

| Action | API |
|--------|-----|
| Assign seat | `POST /api/v1/licenses/seats/assign` |
| Usage | `GET /api/v1/licenses/usage?organizationId=` |
| Entitlement check | `GET /api/v1/licenses/entitlements/check?organizationId=&featureCode=` |

Feature codes commonly used in pricing SKUs: `core.compliance`, `ai.assist`, `governance.risk`, `ace.insights`.

## Trial provision shortcut

Platform admins can bootstrap a trial:

```http
POST /api/v1/commercial/provision
```

Returns key + onboarding checklist (activate → seats → onboard → entitlements → heartbeat → health).

## Status meanings

| Status | Meaning | Action |
|--------|---------|--------|
| Active | Operational | None |
| Grace | Past expiry, within grace days | Renew key (manual) |
| Expired | Not operational | Issue new key; re-activate |
| Revoked | Intentionally disabled | Contact AOVIAS commercial |

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Key not found | Org ID mismatch; typo in keyCode |
| Validation failed | Revoked key; bad offline payload |
| Soft feature blocked | Entitlement missing; header `X-Require-Entitlement` |
| Need support pack | `GET /api/v1/commercial/diagnostics?organizationId=` |

See also: [`docs/commercial-readiness/phase-6/01-licensing-platform.md`](../../docs/commercial-readiness/phase-6/01-licensing-platform.md).
