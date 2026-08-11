# EIWAM Integration Guide

## Getting started

1. Ensure `Integration.*` permissions assigned to operator role via Access Governance.
2. Navigate to **Enterprise Integration** (`/app/integration`) in WebApp.
3. Create a connector, mapping profile, sync job, then run manual sync.

## Connector setup

### Tier 1 (production-ready)

| Provider | Use case | Configuration |
|----------|----------|---------------|
| `csv-import` | Bulk employee load | `{ "content": "header\\nrow..." }` |
| `json-import` | API snapshot import | `{ "records": [...] }` |
| `csv-export` | Employment export | Target entity filter |
| `webhook-inbound` | Real-time HRMS push | Subscription + secret |
| `webhook-outbound` | Notify external systems | Target URL + event filters |

### Tier 2 (reference)

| Provider | Notes |
|----------|-------|
| `ldap` | Requires `host`, `baseDn`; sample pull included |
| `azure-ad` | Requires `tenantId`; sample pull included |

### Tier 3 (stubs)

HRMS, Payroll, HIS, EMR, etc. — validate-only configuration; extend per `EIWAM_EXTENSION_GUIDE.md`.

## Connector lifecycle

```
Draft → Configured → Validated → Connected → (Disconnected | Suspended) → Archived
```

Use **Validate & Connect** (`POST /connectors/{id}/validate`) after configuration changes.

## Mapping profiles

Create field mappings in UI or API:

| External field | Platform target |
|----------------|-----------------|
| ExternalEmployeeNumber | Employment.EmployeeNumber |
| ExternalDepartment | Organization.Department |
| ExternalDesignation | Workforce.PositionTitle |

Attach transformation rules for trim, date parse, lookup tables.

## Synchronization

1. Create `SynchronizationJob` linking source connector + mapping profile.
2. Set direction (`OneWay` / `TwoWay`), sync type (`Full` / `Incremental`).
3. Optional cron schedule for automated runs.
4. Manual run: `POST /synchronizations/run?jobId=...`.

### Conflict policies

| Policy | Behavior |
|--------|----------|
| ExternalWins | Always apply external record |
| PlatformWins | Skip if platform newer |
| ManualReview | Log conflict, skip write |

## Webhooks

### Inbound

1. Create inbound subscription; note subscription ID.
2. Configure external system to POST to Gateway:
   `POST /api/v1/integration/webhooks/inbound/{subscriptionId}`
3. Include `X-Praman-Signature` HMAC header.

### Outbound

1. Create outbound subscription with target URL and event filter JSON.
2. Events from sync completion or platform event consumer trigger delivery.

## Import / export wizards

**Import:** create → preview → execute (with rollback checkpoint).

**Export:** create → execute (CSV/Excel/JSON output).

## Monitoring

- **Connector Health** dashboard — latency, status per connector.
- **Integration Audit** — searchable operation log with trace IDs.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Sync 0 records | Connector configuration, mapping profile fields |
| 403 Forbidden | AG permissions for operator role |
| Webhook 401 | Signature mismatch, subscription disabled |
| Health Degraded | Provider-specific connection settings |
