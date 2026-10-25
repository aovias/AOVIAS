# KB-002 — Activate a License Key

## Symptoms

- Features blocked / entitlement checks fail  
- Status Expired or “No active license”  
- Offline activation validation failed  

## Customer steps (UI)

1. Sign in as ORG_ADMIN / platform admin.  
2. Open **Settings → Platform → Licensing**.  
3. Enter the key code from AOVIAS CS.  
4. Activate (**Online** unless CS specified offline).  
5. Assign seats to users.  
6. Confirm status **Active**.

## API reference

See [`../../licensing/activation-guide.md`](../../licensing/activation-guide.md).

```http
POST /api/v1/licenses/activate
GET  /api/v1/licenses/status?organizationId=
POST /api/v1/licenses/heartbeat
```

Offline: set `"offline": true` and include `offlinePayload` from the commercial ticket.

## Common failures

| Error | Fix |
|-------|-----|
| Key not found | Wrong organizationId or typo |
| Validation failed | Revoked key or bad offline payload |
| Grace / Expired | Request renewal key — [../../licensing/renewal-process.md](../../licensing/renewal-process.md) |
| Seat assignment fails | MaxSeats exceeded |

## Trial shortcut

Platform admin: `POST /api/v1/commercial/provision` then complete returned checklist.
