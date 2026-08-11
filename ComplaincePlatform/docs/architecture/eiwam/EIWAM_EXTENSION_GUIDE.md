# EIWAM Extension Guide — Connector Providers

## Adding a new connector provider

### 1. Implement `IConnectorProvider`

```csharp
public sealed class MyHrmsConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "my-hrms";

    public override Task<ConnectorValidationResult> ValidateConfigurationAsync(
        string configurationJson, CancellationToken ct = default)
    {
        // Parse JSON, return errors for missing required fields
    }

    public override Task<ConnectorHealthResult> CheckHealthAsync(
        IntegrationConnector connector, CancellationToken ct = default)
    {
        // Test connectivity, return latency + status
    }

    public override Task<ConnectorPullResult> PullAsync(
        IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
    {
        // Return ExternalRecord list; set Watermark for incremental sync
    }
}
```

### 2. Register in DI

In `IntegrationService/Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton<IConnectorProvider, MyHrmsConnectorProvider>();
```

### 3. Document configuration schema

Add to integration guide with required JSON properties and example.

### 4. Add tests

- Configuration validation (valid/invalid)
- Health check (mocked HTTP)
- Pull sample records
- End-to-end sync with mocked `IPlatformWriteClient`

### 5. Optional: upgrade from Tier 3 stub

Remove from `ConnectorProviderRegistration.StubTypes` if replacing a stub.

## Mapping extensions

- Add transformation rule types in `MappingEngine.ApplyRule` switch.
- Store rule definitions in `TransformationRule.RulesJson`.
- Seed example rules via `IntegrationSeedData` if platform-wide.

## HTTP write client extensions

Extend `IPlatformWriteClient` / `PlatformWriteClient` for new target entities:

```csharp
Task UpsertOrganizationUnitAsync(Guid orgId, MappedRecord record, CancellationToken ct);
```

Never add DbContext references to other services.

## Webhook event types

1. Add event type string to outbound filter documentation.
2. Emit from sync engine or event consumer when condition met.
3. Register in `EIWAM_EVENT_CATALOG.md`.

## Gateway exposure

New management APIs require:

1. IntegrationService controller endpoint
2. Gateway `IntegrationController` + MediatR proxy handler
3. `Integration.*` permission (seed in AG)
4. Angular UI route if operator-facing

## Checklist

- [ ] Provider implements validate, health, pull
- [ ] DI registration
- [ ] Unit tests (≥3 per provider)
- [ ] Configuration schema documented
- [ ] No cross-service DbContext usage
- [ ] Audit logging on sync operations
- [ ] Outbox events for significant state changes
