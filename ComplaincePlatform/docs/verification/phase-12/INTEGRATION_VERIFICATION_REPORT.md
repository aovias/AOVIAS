# Phase 12 — Integration Verification Report

## HTTP client integrations

| Target service | Config in appsettings | HttpClient registered | Methods implemented | Status |
|----------------|----------------------|----------------------|---------------------|--------|
| WorkforceService | ✅ BaseUrl 7204 | ✅ `IPlatformWriteClient` | `UpsertEmploymentAsync` | ⚠️ Partial |
| PersonService | ✅ BaseUrl 7203 | ❌ | — | ❌ Missing |
| OrganizationService | ✅ BaseUrl 7202 | ❌ | — | ❌ Missing |
| AccessGovernanceService | ✅ BaseUrl 7205 | ⚠️ Via `AddPramanAuthorization` | Evaluate permissions | ✅ |

## PlatformWriteClient behavior

```csharp
// IntegrationService/Infrastructure/Clients/PlatformWriteClient.cs
await http.PostAsJsonAsync("/api/v1/employment", body, ct);
// catch { /* best effort — exceptions swallowed */ }
```

**Finding:** Violates observability and sync accuracy requirements. Failures not propagated to `SynchronizationRun.RecordsFailed`.

## Gateway integration

| Check | Status |
|-------|--------|
| HttpClient "IntegrationService" registered | ✅ |
| BaseUrl in Gateway appsettings | ✅ `http://integrationservice:7218` |
| ProxyToIntegrationServiceAsync | ✅ |
| Correlation forwarding | ✅ |
| All routes proxied | ✅ |

## OutboxRelayWorker

| Check | Status |
|-------|--------|
| IntegrationConnection string | ✅ appsettings |
| IntegrationOutboxRelayBackgroundService | ✅ |
| Topic `integration-events` | ✅ |
| Build (after fix) | ✅ `using OutboxRelayWorker` added |

## Service Bus consumers (IntegrationService)

| Subscription | Plan | Implemented |
|--------------|------|-------------|
| workforce-events | ✅ | ✅ `IntegrationEventConsumerHostedService` |
| directory-events | ✅ | ❌ Not subscribed |
| compliance-events | ✅ | ❌ Not subscribed |

Consumer disabled when `ServiceBus:ConnectionString` empty — expected in dev.

## Event publishers (outbox)

| Event | Published |
|-------|-----------|
| SynchronizationStarted | ✅ |
| SynchronizationCompleted | ✅ |
| ConnectorConnected | ✅ |
| ImportCompleted | ✅ |
| ExportCompleted | ✅ |
| WebhookDelivered | ⚠️ Audit only, not outbox |
| ConnectorHealthChanged | ❌ |
| ConnectorDisconnected | ❌ |

## Circular dependencies

- ✅ IntegrationService references Shared.* only + no domain service project refs
- ✅ Gateway references no Integration domain types

## Incorrect URLs

- IntegrationService registers Workforce client but posts to `/api/v1/employment` — verify WorkforceService route exists (assumed correct per plan).

## Architecture rule compliance

| Rule | Status |
|------|--------|
| No cross-service DbContext | ✅ |
| Outbox not direct RabbitMQ | ✅ |
| Gateway HTTP only | ✅ |
| ComplianceJourney not touched | ✅ |

## Verdict

Gateway and outbox relay integrations are **sound**. Downstream HTTP write integration is **incomplete** (1 of 3 target services, silent failures).
