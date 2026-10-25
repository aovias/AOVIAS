# EIWAM Event Catalog

## Service Bus topics

| Topic | Source | Purpose |
|-------|--------|---------|
| `compliance-events` | ComplianceService | Journey transitions, score updates |
| `workforce-events` | WorkforceService | Employment changes |
| `directory-events` | EnterpriseDirectoryService | Index rebuilds, search updates |
| `integration-events` | IntegrationService | Sync, connector, webhook, import/export |

## Integration event types

| Event type | Trigger | Payload summary |
|------------|---------|-----------------|
| `SynchronizationStarted` | Sync run begins | jobId, runId |
| `SynchronizationCompleted` | Sync run success | jobId, runId, recordsProcessed |
| `SynchronizationFailed` | Sync run exhausted retries | jobId, runId, recordsFailed |
| `ConnectorConnected` | Validate/connect success | connectorId |
| `ConnectorDisconnected` | Lifecycle → Disconnected/Suspended | connectorId, lifecycleState |
| `WebhookDelivered` | Outbound success | subscriptionId, eventType |
| `ImportCompleted` | Import execute done | importId, recordsImported |
| `ImportRolledBack` | Import rollback executed | importId, rolledBackCount |
| `ExportCompleted` | Export execute done | exportId, recordsExported |
| `ConnectorHealthChanged` | Health check transition | connectorId, status |

## Directory event types (selected)

| Event type | Purpose |
|------------|---------|
| `SearchIndexRebuilt` | Full org rebuild complete |
| `DirectoryIndexUpdated` | Incremental upsert |

## Compliance event types (selected)

| Event type | Purpose |
|------------|---------|
| `ComplianceStateChangedDomainEvent` | Lifecycle transition |
| `ComplianceScoreUpdatedDomainEvent` | Score projection |

## Consumer patterns

| Consumer | Subscriptions |
|----------|---------------|
| `IntegrationEventConsumerHostedService` | workforce-events, directory-events, compliance-events |
| `DirectoryEventConsumerHostedService` | workforce-events |
| Communication event processor | compliance-events, integration-events |

## Outbox message lifecycle

```
Created → ProcessedOn (local processor) → RelayProcessedOn (OutboxRelayWorker)
```

## Idempotency

- Service Bus `MessageId` = outbox message GUID
- `CorrelationId` = aggregate ID
- `Subject` = event type string

## Future topics

- `ai-events` — AI workflow completion (planned)
- `governance-events` — committee decisions (planned)
