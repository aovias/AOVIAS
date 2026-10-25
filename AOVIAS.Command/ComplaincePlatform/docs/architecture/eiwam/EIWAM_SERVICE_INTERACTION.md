# EIWAM Service Interaction Matrix

## HTTP interactions (synchronous)

| From | To | Purpose | Auth |
|------|-----|---------|------|
| WebApp | Gateway | All API calls | JWT |
| Gateway | IntegrationService | `/api/v1/integration/*` proxy | JWT forward |
| Gateway | EnterpriseDirectoryService | `/api/v1/directory/*` | JWT forward |
| Gateway | ComplianceService | Journey, evidence, tasks | JWT forward |
| IntegrationService | PersonService | Upsert persons | Internal API key |
| IntegrationService | WorkforceService | Upsert employment | Internal API key |
| IntegrationService | OrganizationService | Department/membership | Internal API key |
| IntegrationService | AccessGovernanceService | Permission evaluation | Internal API key |
| EnterpriseDirectoryService | Person/Workforce/Professional | Index hydration | Internal API key |
| AIOrchestratorService | Compliance/KnowledgeBase | AI context | Internal API key |

## Event interactions (asynchronous)

| Topic | Producers | Consumers |
|-------|-----------|-----------|
| `compliance-events` | ComplianceService outbox | Gateway, analytics |
| `workforce-events` | WorkforceService outbox | Directory, Integration |
| `directory-events` | EnterpriseDirectoryService outbox | Analytics |
| `integration-events` | IntegrationService outbox | Communication, monitoring |

## Outbox relay pattern

1. Domain action persists aggregate + `OutboxMessage`.
2. Local `OutboxProcessor` marks `ProcessedOn`.
3. `OutboxRelayWorker` reads processed messages, publishes to Service Bus, sets `RelayProcessedOn`.

## Correlation

All HTTP calls propagate `X-Correlation-ID`. Sync runs and integration audit entries store `traceId`.

## Failure modes

| Failure | Mitigation |
|---------|------------|
| Downstream HTTP timeout | Sync retry with exponential backoff |
| Webhook delivery failure | Retry + DLQ in `WebhookDelivery` |
| Service Bus unavailable | Outbox relay retries; messages remain until relay succeeds |
| AG denial | 403 via `ApiResponseFactory`; no partial writes |

## Gateway route prefixes

| Prefix | Target service |
|--------|----------------|
| `/api/v1/integration` | IntegrationService |
| `/api/v1/directory` | EnterpriseDirectoryService |
| `/api/v1/intelligence` | WorkforceIntelligenceService |
| `/api/v1/compliance` | ComplianceService |

See `EIWAM_API_CATALOG.md` for full route listing.
