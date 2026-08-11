using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Events;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace ComplianceService.Application.Dashboard.Projection;

public sealed class DashboardProjectionEngine : IDashboardProjectionEngine
{
    private readonly IDashboardProjectionRepository _repository;
    private readonly DashboardProjectionUpdater _updater;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DashboardProjectionOptions _options;
    private readonly ILogger<DashboardProjectionEngine> _logger;

    public DashboardProjectionEngine(
        IDashboardProjectionRepository repository,
        DashboardProjectionUpdater updater,
        IServiceScopeFactory scopeFactory,
        IOptions<DashboardProjectionOptions> options,
        ILogger<DashboardProjectionEngine> logger)
    {
        _repository = repository;
        _updater = updater;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var scopes = await ResolveScopesAsync(message, cancellationToken);
            if (scopes.Count == 0)
            {
                return;
            }

            foreach (var scope in scopes)
            {
                if (RequiresFullRebuild(message.Type))
                {
                    await RebuildAsync(scope, message.Type, message.Id, cancellationToken);
                }
                else
                {
                    await _updater.ApplyIncrementalAsync(scope, message, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard projection update failed for outbox message {MessageId} type {Type}", message.Id, message.Type);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 200)
            {
                _logger.LogInformation(
                    "Dashboard projection event processed in {ElapsedMs}ms for {EventType}",
                    stopwatch.ElapsedMilliseconds,
                    message.Type);
            }
        }
    }

    public async Task RebuildAsync(
        DashboardProjectionScope scope,
        string triggerEventType,
        Guid? outboxMessageId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || scope.IsTenantWide)
        {
            return;
        }

        var projection = await _repository.GetOrCreateAsync(scope, cancellationToken);
        if (outboxMessageId.HasValue && projection.LastProcessedOutboxMessageId == outboxMessageId)
        {
            return;
        }

        var rebuildStopwatch = Stopwatch.StartNew();
        var organizationId = scope.OrganizationId != Guid.Empty
            ? scope.OrganizationId
            : await ResolveTenantIdAsync(scope.HospitalId, scope.FrameworkId, cancellationToken);

        if (organizationId == Guid.Empty)
        {
            return;
        }

        using var serviceScope = _scopeFactory.CreateScope();
        var builder = serviceScope.ServiceProvider.GetRequiredService<IDashboardSnapshotBuilder>();

        var hospitalId = scope.HospitalId;
        var frameworkId = scope.FrameworkId == Guid.Empty ? (Guid?)null : scope.FrameworkId;

        var snapshot = await builder.BuildLiveAsync(
            organizationId,
            hospitalId,
            frameworkId,
            cancellationToken);

        DashboardProjectionSerializer.ApplySnapshotDto(projection, snapshot);
        projection.OrganizationId = organizationId;
        projection.DataVersion += 1;
        projection.LastEventProcessedAt = DateTime.UtcNow;
        projection.LastEventType = triggerEventType;
        projection.LastProcessedOutboxMessageId = outboxMessageId;

        await _repository.SaveAsync(projection, cancellationToken);

        rebuildStopwatch.Stop();
        _logger.LogDebug(
            "Dashboard projection rebuilt for org={OrganizationId} hospital={HospitalId} framework={FrameworkId} in {ElapsedMs}ms",
            scope.OrganizationId,
            scope.HospitalId,
            scope.FrameworkId,
            rebuildStopwatch.ElapsedMilliseconds);
    }

    public Task<DashboardProjection?> TryGetProjectionAsync(
        DashboardProjectionScope scope,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult<DashboardProjection?>(null);
        }

        return _repository.GetAsync(scope, cancellationToken);
    }

    private async Task<Guid> ResolveTenantIdAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();
        var query = dbContext.ComplianceJourneys.AsNoTracking().Where(j => j.HospitalId == hospitalId);
        if (frameworkId != Guid.Empty)
        {
            query = query.Where(j => j.FrameworkId == frameworkId);
        }

        return await query.Select(j => j.TenantId).FirstOrDefaultAsync(cancellationToken);
    }

    private static bool RequiresFullRebuild(string eventType)
    {
        if (eventType.Contains(nameof(ComplianceScoreUpdatedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (eventType.Contains(nameof(ComplianceStateChangedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (eventType.Contains("ComplianceEnrolled", StringComparison.OrdinalIgnoreCase)
            || eventType.Contains("ComplianceApproved", StringComparison.OrdinalIgnoreCase)
            || eventType.Contains("ComplianceRenewed", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (eventType.Contains(nameof(ObligationStatusUpdatedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (eventType.StartsWith("ComplianceEvent.", StringComparison.OrdinalIgnoreCase))
        {
            return eventType.Contains("ComplianceEvaluated", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private async Task<IReadOnlyList<DashboardProjectionScope>> ResolveScopesAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Type.StartsWith("ComplianceEvent.", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveComplianceEventScopes(message);
        }

        if (message.Type.Contains(nameof(ComplianceScoreUpdatedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            var evt = JsonSerializer.Deserialize<ComplianceScoreUpdatedDomainEvent>(message.Payload);
            if (evt is null)
            {
                return [];
            }

            var tenantId = await ResolveTenantIdAsync(evt.HospitalId, evt.FrameworkId, cancellationToken);
            return [DashboardProjectionScope.ForHospital(tenantId, evt.HospitalId, evt.FrameworkId)];
        }

        if (message.Type.Contains(nameof(ObligationStatusUpdatedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            var evt = JsonSerializer.Deserialize<ObligationStatusUpdatedDomainEvent>(message.Payload);
            return evt is null
                ? []
                : [DashboardProjectionScope.ForHospital(evt.OrganizationId, evt.HospitalId, evt.FrameworkId)];
        }

        if (message.Type.Contains(nameof(DeficiencyIdentifiedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            var evt = JsonSerializer.Deserialize<DeficiencyIdentifiedDomainEvent>(message.Payload);
            if (evt is null)
            {
                return [];
            }

            var tenantId = await ResolveTenantIdAsync(evt.HospitalId, evt.FrameworkId, cancellationToken);
            return [DashboardProjectionScope.ForHospital(tenantId, evt.HospitalId, evt.FrameworkId)];
        }

        if (message.Type.Contains(nameof(DeficiencyClosedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            var evt = JsonSerializer.Deserialize<DeficiencyClosedDomainEvent>(message.Payload);
            if (evt is null)
            {
                return [];
            }

            var tenantId = await ResolveTenantIdAsync(evt.HospitalId, evt.FrameworkId, cancellationToken);
            return [DashboardProjectionScope.ForHospital(tenantId, evt.HospitalId, evt.FrameworkId)];
        }

        if (message.Type.Contains(nameof(DeficiencyStatusChangedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            var evt = JsonSerializer.Deserialize<DeficiencyStatusChangedDomainEvent>(message.Payload);
            if (evt is null)
            {
                return [];
            }

            var tenantId = await ResolveTenantIdAsync(evt.HospitalId, evt.FrameworkId, cancellationToken);
            return [DashboardProjectionScope.ForHospital(tenantId, evt.HospitalId, evt.FrameworkId)];
        }

        if (message.Type.Contains(nameof(ComplianceStateChangedDomainEvent), StringComparison.OrdinalIgnoreCase))
        {
            var evt = JsonSerializer.Deserialize<ComplianceStateChangedDomainEvent>(message.Payload);
            return evt is null
                ? []
                : [DashboardProjectionScope.ForHospital(evt.TenantId, evt.HospitalId, evt.FrameworkId)];
        }

        return ResolveJourneyLifecycleScopes(message);
    }

    private static IReadOnlyList<DashboardProjectionScope> ResolveComplianceEventScopes(OutboxMessage message)
    {
        var envelope = JsonSerializer.Deserialize<ComplianceEventEnvelope>(message.Payload);
        if (envelope is null)
        {
            return [];
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(envelope.Payload) ? "{}" : envelope.Payload);
        var root = doc.RootElement;

        var hospitalId = TryReadGuid(root, "HospitalId") ?? Guid.Empty;
        var frameworkId = TryReadGuid(root, "FrameworkId");
        var organizationId = envelope.TenantId != Guid.Empty ? envelope.TenantId : Guid.Empty;

        if (hospitalId == Guid.Empty)
        {
            return [];
        }

        return [DashboardProjectionScope.ForHospital(organizationId, hospitalId, frameworkId)];
    }

    private static IReadOnlyList<DashboardProjectionScope> ResolveJourneyLifecycleScopes(OutboxMessage message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message.Payload);
            var root = doc.RootElement;
            var hospitalId = TryReadGuid(root, "HospitalId") ?? Guid.Empty;
            var frameworkId = TryReadGuid(root, "FrameworkId");
            var tenantId = TryReadGuid(root, "TenantId") ?? TryReadGuid(root, "OrganizationId") ?? Guid.Empty;

            if (hospitalId == Guid.Empty)
            {
                return [];
            }

            return [DashboardProjectionScope.ForHospital(tenantId, hospitalId, frameworkId)];
        }
        catch
        {
            return [];
        }
    }

    private static Guid? TryReadGuid(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
