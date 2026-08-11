using System.Text.Json;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Domain.ReadModels;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Infrastructure.Services;

public sealed class ConnectorAppService(
    IntegrationDbContext db,
    IConnectorRegistry registry,
    ICredentialProtector credentialProtector,
    IIntegrationOutboxWriter outbox)
{
    public async Task<ConnectorDto> CreateAsync(CreateConnectorRequest request, CancellationToken ct = default)
    {
        var provider = registry.Resolve(request.ProviderType);
        var validation = await provider.ValidateConfigurationAsync(request.ConfigurationJson, ct);
        if (!validation.IsValid) throw new InvalidOperationException(string.Join("; ", validation.Errors));

        Guid? credId = null;
        if (request.Credential is not null)
        {
            var cred = new ConnectorCredential
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Name = request.Credential.Name,
                CredentialType = request.Credential.CredentialType,
                EncryptedPayload = credentialProtector.Protect(request.Credential.Secret)
            };
            db.ConnectorCredentials.Add(cred);
            credId = cred.Id;
        }

        var connector = new IntegrationConnector
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            ProviderType = request.ProviderType,
            ConfigurationJson = request.ConfigurationJson,
            CredentialId = credId,
            LifecycleState = "Configured"
        };
        db.Connectors.Add(connector);
        await db.SaveChangesAsync(ct);
        return Map(connector);
    }

    public async Task<ConnectorDto?> UpdateAsync(Guid id, UpdateConnectorRequest request, CancellationToken ct = default)
    {
        var c = await db.Connectors.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (c is null) return null;
        var previousLifecycle = c.LifecycleState;
        var previousHealth = c.HealthStatus;
        c.Name = request.Name;
        c.ConfigurationJson = request.ConfigurationJson;
        c.LifecycleState = request.LifecycleState;
        c.Version++;
        c.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (!string.Equals(previousHealth, c.HealthStatus, StringComparison.OrdinalIgnoreCase))
            await outbox.EnqueueAsync("ConnectorHealthChanged", c.Id, new { connectorId = c.Id, previousHealth, c.HealthStatus }, ct);
        if (IsDisconnectedLifecycle(c.LifecycleState) && !IsDisconnectedLifecycle(previousLifecycle))
            await outbox.EnqueueAsync("ConnectorDisconnected", c.Id, new { connectorId = c.Id, c.LifecycleState }, ct);
        return Map(c);
    }

    public async Task<IReadOnlyList<ConnectorDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var items = await db.Connectors.AsNoTracking().Where(c => c.OrganizationId == organizationId && !c.IsDeleted).ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<ConnectorDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Connectors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return c is null ? null : Map(c);
    }

    public async Task<ConnectorHealthResult> ValidateAndConnectAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Connectors.FirstAsync(x => x.Id == id, ct);
        var provider = registry.Resolve(c.ProviderType);
        var validation = await provider.ValidateConfigurationAsync(c.ConfigurationJson, ct);
        if (!validation.IsValid) { c.LifecycleState = "Configured"; c.HealthStatus = "Unhealthy"; await db.SaveChangesAsync(ct); throw new InvalidOperationException(string.Join("; ", validation.Errors)); }
        c.LifecycleState = "Validated";
        var health = await provider.CheckHealthAsync(c, ct);
        c.LifecycleState = "Connected";
        c.HealthStatus = health.Status;
        db.ConnectorHealthRecords.Add(new ConnectorHealth
        {
            Id = Guid.NewGuid(), ConnectorId = c.Id, Status = health.Status,
            LatencyMs = health.LatencyMs, ErrorSummary = health.Message, CheckedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        await outbox.EnqueueAsync("ConnectorConnected", c.Id, new { connectorId = c.Id }, ct);
        return health;
    }

    private static ConnectorDto Map(IntegrationConnector c) => new()
    {
        Id = c.Id, OrganizationId = c.OrganizationId, Name = c.Name, ProviderType = c.ProviderType,
        LifecycleState = c.LifecycleState, HealthStatus = c.HealthStatus, Version = c.Version,
        ConfigurationJson = c.ConfigurationJson, CreatedAtUtc = c.CreatedAtUtc
    };

    private static bool IsDisconnectedLifecycle(string lifecycle) =>
        lifecycle is "Disconnected" or "Suspended";
}

public sealed class ConnectorHealthService(
    IntegrationDbContext db,
    IConnectorRegistry registry,
    IIntegrationOutboxWriter outbox)
{
    public async Task<IReadOnlyList<ConnectorHealthDto>> GetHealthAsync(Guid organizationId, CancellationToken ct = default)
    {
        var connectors = await db.Connectors.Where(c => c.OrganizationId == organizationId && !c.IsDeleted).ToListAsync(ct);
        var result = new List<ConnectorHealthDto>();
        foreach (var c in connectors)
        {
            var previousHealth = c.HealthStatus;
            var provider = registry.Resolve(c.ProviderType);
            var health = await provider.CheckHealthAsync(c, ct);
            db.ConnectorHealthRecords.Add(new ConnectorHealth
            {
                Id = Guid.NewGuid(), ConnectorId = c.Id, Status = health.Status,
                LatencyMs = health.LatencyMs, ErrorSummary = health.Message, CheckedAtUtc = DateTime.UtcNow
            });
            c.HealthStatus = health.Status;
            if (!string.Equals(previousHealth, c.HealthStatus, StringComparison.OrdinalIgnoreCase))
                await outbox.EnqueueAsync("ConnectorHealthChanged", c.Id, new { connectorId = c.Id, previousHealth, c.HealthStatus }, ct);
            result.Add(new ConnectorHealthDto { ConnectorId = c.Id, ConnectorName = c.Name, Status = health.Status, LatencyMs = health.LatencyMs, SuccessRate = 100, CheckedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);
        return result;
    }
}

public sealed class IntegrationAuditService(IntegrationDbContext db) : IIntegrationAuditService
{
    public async Task LogAsync(Guid organizationId, string operation, string result, Guid? userId, Guid? connectorId, string source, string target, int records, long durationMs, string? traceId, string? errorsJson = null, CancellationToken ct = default)
    {
        db.IntegrationAuditLogs.Add(new IntegrationAuditLog
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = userId, ConnectorId = connectorId,
            Operation = operation, Source = source, Target = target, Result = result,
            DurationMs = durationMs, RecordsProcessed = records, ErrorsJson = errorsJson, TraceId = traceId
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IntegrationAuditDto>> ListAsync(Guid organizationId, int take = 100, CancellationToken ct = default)
    {
        return await db.IntegrationAuditLogs.AsNoTracking().Where(a => a.OrganizationId == organizationId)
            .OrderByDescending(a => a.OccurredAtUtc).Take(take)
            .Select(a => new IntegrationAuditDto { Id = a.Id, Operation = a.Operation, Result = a.Result, RecordsProcessed = a.RecordsProcessed, DurationMs = a.DurationMs, TraceId = a.TraceId, OccurredAtUtc = a.OccurredAtUtc })
            .ToListAsync(ct);
    }
}

public sealed class IntegrationOutboxWriter(IntegrationDbContext db) : IIntegrationOutboxWriter
{
    public async Task EnqueueAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(), AggregateId = aggregateId, OccurredOn = DateTime.UtcNow,
            Type = type, Payload = JsonSerializer.Serialize(payload)
        });
        await db.SaveChangesAsync(ct);
    }
}
