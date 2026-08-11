# PRAMAN Integration Layer Domain Model — EIWAM Phase 12

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 12 — Enterprise Integration Layer |
| **Prior milestone** | Phase 11 Enterprise Directory |
| **Status** | Approved — greenfield IntegrationService |
| **Bounded context** | IntegrationService (port 7218) |

## Purpose

The Integration Layer provides **standardized external system connectivity** for PRAMAN. It orchestrates connectors, synchronization, mapping, webhooks, and import/export without owning operational workforce data.

```
External System → ConnectorProvider → MappingEngine → HTTP write → Person/Workforce/Organization
Platform Event → WebhookManager → Outbound delivery
File Upload → ImportExportEngine → Preview → Execute → Audit
```

**Integration is orchestration metadata. Never own source-of-truth aggregates.**

---

## Spec name mapping

| EIWAM Phase 12 spec | Implementation |
|---------------------|----------------|
| IntegrationConnector | `IntegrationConnector` |
| ConnectorProfile | `IntegrationConnector` (named instance) |
| ConnectorConfiguration | `ConnectorConfiguration` JSON on connector |
| ConnectorCredential | `ConnectorCredential` (encrypted) |
| SynchronizationJob | `SynchronizationJob` |
| SynchronizationSchedule | `SynchronizationSchedule` |
| SynchronizationRun | `SynchronizationRun` |
| MappingProfile | `MappingProfile` |
| FieldMapping | `FieldMapping` |
| TransformationRule | `TransformationRule` |
| WebhookSubscription | `WebhookSubscription` |
| WebhookDelivery | `WebhookDelivery` |
| ImportJob / ExportJob | `ImportJob` / `ExportJob` |
| ConnectorHealth | `ConnectorHealth` |
| IntegrationAudit | `IntegrationAuditLog` |

---

## Bounded context boundaries

| Context | Owns | Must NOT own |
|---------|------|--------------|
| **IntegrationService** | Connectors, sync jobs, mappings, webhooks, import/export metadata, audit | Person, Employment, credentials |
| **PersonService** | Person aggregate | Connector config |
| **EnterpriseDirectoryService** | Search index | External sync orchestration |
| **AccessGovernanceService** | Permission evaluation | Connector secrets (only authz) |

---

## Connector lifecycle

```
Draft → Configured → Validated → Connected → (Disconnected | Suspended) → Archived
```

Health: `Healthy`, `Degraded`, `Unhealthy`, `Unknown`

---

## Connector provider tiers

| Tier | Types | Scope |
|------|-------|-------|
| Tier 1 | csv-import, csv-export, json-import, webhook-inbound, webhook-outbound | Full implementation |
| Tier 2 | ldap, azure-ad | Connection test + sample pull |
| Tier 3 | hrms, payroll, attendance, biometric, lms, his, emr, erp, dms, email, sms, whatsapp, teams, slack | Validate-only stubs |
| Future | fhir, hl7, openehr, government-portal | Interface placeholder |

---

## Synchronization modes

| Mode | Field |
|------|-------|
| Direction | `OneWay`, `TwoWay` |
| Sync type | `Full`, `Incremental`, `Manual`, `Scheduled`, `RealTime` |
| Conflict policy | `ExternalWins`, `PlatformWins`, `ManualReview` |

---

## Mapping engine

Transformation rule types: `String`, `Date`, `Number`, `Boolean`, `Lookup`, `DefaultValue`, `Expression`

Example seed mappings (data, not code):
- `ExternalEmployeeNumber` → `Employment.EmployeeNumber`
- `ExternalDepartment` → `Organization.Department`
- `ExternalDesignation` → `Workforce.PositionTitle`

---

## Events

**Publish** (integration outbox → `integration-events`):
- `SynchronizationStarted`, `SynchronizationCompleted`
- `ConnectorConnected`, `ConnectorDisconnected`
- `WebhookDelivered`, `ImportCompleted`, `ExportCompleted`, `ConnectorHealthChanged`

**Subscribe**: `workforce-events`, `directory-events` for webhook triggers and incremental watermarks.

---

## Permissions

- `Integration.Connector.Read`, `Integration.Connector.Manage`
- `Integration.Sync.Read`, `Integration.Sync.Execute`
- `Integration.Mapping.Read`, `Integration.Mapping.Manage`
- `Integration.Webhook.Read`, `Integration.Webhook.Manage`
- `Integration.Import.Execute`, `Integration.Export.Execute`
- `Integration.Monitor.Read`, `Integration.Audit.Read`

---

## API surface

| Route | Purpose |
|-------|---------|
| `/api/v1/connectors` | CRUD connectors |
| `/api/v1/synchronizations` | Jobs and runs |
| `/api/v1/mappings` | Mapping profiles |
| `/api/v1/webhooks` | Subscriptions + inbound |
| `/api/v1/imports` / `/exports` | File operations |
| `/api/v1/connector-health` | Health dashboard |
| `/api/v1/integration-audit` | Audit log |
