# Phase 12 — Database Verification Report

**Database:** `PramanIntegration`  
**Migration:** `20260707080105_M12_IntegrationFoundation`

## Tables

| Table | In migration | In DbContext | Indexes | Notes |
|-------|--------------|--------------|---------|-------|
| IntegrationConnectors | ✅ | ✅ | Org+Name | RowVersion configured |
| ConnectorCredentials | ✅ | ✅ | — | EncryptedPayload nvarchar(max) |
| SynchronizationJobs | ✅ | ✅ | — | |
| SynchronizationSchedules | ✅ | ✅ | JobId | |
| SynchronizationRuns | ✅ | ✅ | JobId | |
| MappingProfiles | ✅ | ✅ | — | |
| FieldMappings | ✅ | ✅ | ProfileId | |
| TransformationRules | ✅ | ✅ | — | |
| WebhookSubscriptions | ✅ | ✅ | — | |
| WebhookDeliveries | ✅ | ✅ | SubscriptionId | |
| ImportJobs | ✅ | ✅ | — | RollbackCheckpointJson column present |
| ExportJobs | ✅ | ✅ | — | |
| ConnectorHealth | ✅ | ✅ | ConnectorId | SuccessRate decimal(18,2) in migration |
| IntegrationAuditLogs | ✅ | ✅ | Org+OccurredAtUtc | |
| OutboxMessages | ✅ | ✅ | — | RelayProcessedOn present |

## Missing from plan

| Planned model | Status |
|---------------|--------|
| ConnectorProfile (separate) | Not created — merged into IntegrationConnector |
| ConflictRecord | Not persisted — conflicts counted on run only |

## Relationships

- ⚠️ No EF FK constraints between connectors/jobs/mappings (GUID refs only) — acceptable for microservice read models
- ✅ No cross-database FKs

## Concurrency

- ✅ `RowVersion` on `IntegrationConnector`
- ❌ Other entities lack optimistic concurrency tokens

## Soft delete

- ✅ `IsDeleted` on Connector, WebhookSubscription, MappingProfile
- ❌ Not on all entities (jobs, runs use hard rows)

## Audit columns

- ✅ `CreatedAtUtc`, `UpdatedAtUtc` on connector
- ⚠️ Inconsistent across all tables

## PlatformId

- N/A — org-scoped via `OrganizationId` Guid

## Orphan risk

- Deleting connector does not cascade-delete sync jobs (soft delete only on connector)
- Credential orphan possible if connector hard-deleted (soft delete mitigates)

## Migration quality

- ✅ Single foundation migration creates all tables
- ⚠️ EF warning: SuccessRate precision — migration has decimal(18,2) but model config could use `HasPrecision`

## Duplicate tables

- ✅ None — dedicated Integration DB only
