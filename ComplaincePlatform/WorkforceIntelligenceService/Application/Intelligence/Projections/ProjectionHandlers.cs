using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Application.Intelligence.Projections;

public static class IntelligenceEventPayloadParser
{
    public static Guid? ExtractOrganizationId(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("organizationId", out var prop) && Guid.TryParse(prop.GetString(), out var id))
                return id;
        }
        catch { /* ignore */ }
        return null;
    }
}

public abstract class IntelligenceProjectionHandlerBase(
    WorkforceIntelligenceDbContext db,
    ILogger logger) : IIntelligenceProjectionHandler
{
    public abstract string EventType { get; }

    public abstract Task HandleAsync(IntelligenceEventEnvelope envelope, CancellationToken ct = default);

    protected async Task TouchCheckpointAsync(string eventType, string? messageId, CancellationToken ct)
    {
        var cp = await db.ProjectionCheckpoints.FirstOrDefaultAsync(c => c.EventType == eventType, ct);
        if (cp == null)
        {
            db.ProjectionCheckpoints.Add(new Domain.ReadModels.ProjectionCheckpoint
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                LastProcessedAtUtc = DateTime.UtcNow,
                LastMessageId = messageId
            });
        }
        else
        {
            cp.LastProcessedAtUtc = DateTime.UtcNow;
            cp.LastMessageId = messageId;
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class SnapshotRefreshProjection(
    WorkforceIntelligenceDbContext db,
    IWorkforceSnapshotService snapshotService,
    ILogger<SnapshotRefreshProjection> logger,
    string eventType) : IntelligenceProjectionHandlerBase(db, logger)
{
    public override string EventType { get; } = eventType;

    public override async Task HandleAsync(IntelligenceEventEnvelope envelope, CancellationToken ct = default)
    {
        var orgId = IntelligenceEventPayloadParser.ExtractOrganizationId(envelope.Payload);
        if (orgId.HasValue)
            await snapshotService.RefreshOrganizationAsync(orgId.Value, envelope.AggregateId.ToString(), ct: ct);
        await TouchCheckpointAsync(EventType, envelope.AggregateId.ToString(), ct);
    }
}

public sealed class CertificationExpiredProjection(
    WorkforceIntelligenceDbContext db,
    IRecommendationOrchestrator recommendations,
    ILogger<CertificationExpiredProjection> logger) : IntelligenceProjectionHandlerBase(db, logger)
{
    public override string EventType => "CertificationExpired";

    public override async Task HandleAsync(IntelligenceEventEnvelope envelope, CancellationToken ct = default)
    {
        var orgId = IntelligenceEventPayloadParser.ExtractOrganizationId(envelope.Payload);
        if (orgId.HasValue)
            await recommendations.GenerateAsync(orgId.Value, envelope.AggregateId.ToString(), ct);
        await TouchCheckpointAsync(EventType, envelope.AggregateId.ToString(), ct);
    }
}
