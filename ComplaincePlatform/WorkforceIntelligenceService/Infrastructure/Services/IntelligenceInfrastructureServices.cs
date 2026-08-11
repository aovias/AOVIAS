using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Domain.ReadModels;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Infrastructure.Services;

public sealed class AnalyticsCacheService(WorkforceIntelligenceDbContext db) : IAnalyticsCacheService
{
    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken ct = default)
    {
        var entry = await db.AnalyticsCaches.FirstOrDefaultAsync(c => c.CacheKey == cacheKey && c.ExpiresAtUtc > DateTime.UtcNow, ct);
        if (entry == null) return default;
        return JsonSerializer.Deserialize<T>(entry.PayloadJson);
    }

    public async Task SetAsync<T>(string cacheKey, T payload, TimeSpan ttl, CancellationToken ct = default)
    {
        var existing = await db.AnalyticsCaches.FirstOrDefaultAsync(c => c.CacheKey == cacheKey, ct);
        if (existing != null) db.AnalyticsCaches.Remove(existing);
        db.AnalyticsCaches.Add(new AnalyticsCache
        {
            Id = Guid.NewGuid(),
            CacheKey = cacheKey,
            PayloadJson = JsonSerializer.Serialize(payload),
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task InvalidateOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var prefix = organizationId.ToString();
        var entries = await db.AnalyticsCaches.Where(c => c.CacheKey.Contains(prefix)).ToListAsync(ct);
        db.AnalyticsCaches.RemoveRange(entries);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class IntelligenceOutboxWriter(WorkforceIntelligenceDbContext db) : IIntelligenceOutboxWriter
{
    public async Task EnqueueAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            OccurredOn = DateTime.UtcNow,
            Type = type,
            Payload = JsonSerializer.Serialize(payload)
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class IntelligenceProjectionRegistry(IEnumerable<IIntelligenceProjectionHandler> handlers) : IIntelligenceProjectionRegistry
{
    public IReadOnlyList<IIntelligenceProjectionHandler> Handlers { get; } = handlers.ToList();

    public async Task ProcessAsync(IntelligenceEventEnvelope envelope, CancellationToken ct = default)
    {
        var handler = Handlers.FirstOrDefault(h => h.EventType == envelope.Type);
        if (handler != null) await handler.HandleAsync(envelope, ct);
    }
}
