using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Copilot.Repositories;

public sealed class CopilotActivityRepository : ICopilotActivityRepository
{
    private readonly ComplianceDbContext _db;

    public CopilotActivityRepository(ComplianceDbContext db) => _db = db;

    public async Task RecordAsync(CopilotActivityEvent activity, CancellationToken ct = default) =>
        await _db.CopilotActivityEvents.AddAsync(activity, ct);

    public async Task<IReadOnlyList<CopilotActivityEvent>> GetRecentAsync(
        Guid organizationId,
        int limit = 50,
        CancellationToken ct = default) =>
        await _db.CopilotActivityEvents
            .Where(e => e.OrganizationId == organizationId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CopilotActivityEvent>> GetByDateRangeAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? eventType = null,
        string? persona = null,
        CancellationToken ct = default)
    {
        var q = _db.CopilotActivityEvents.Where(e => e.OrganizationId == organizationId);
        if (fromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(e => e.OccurredAtUtc < toUtc.Value);
        if (!string.IsNullOrWhiteSpace(eventType)) q = q.Where(e => e.EventType == eventType);
        if (!string.IsNullOrWhiteSpace(persona)) q = q.Where(e => e.Persona == persona);

        return await q.OrderByDescending(e => e.OccurredAtUtc).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivityAggregationRow>> AggregateByEventTypeAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var q = _db.CopilotActivityEvents.Where(e => e.OrganizationId == organizationId);
        if (fromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(e => e.OccurredAtUtc < toUtc.Value);

        return await q
            .GroupBy(e => e.EventType)
            .Select(g => new ActivityAggregationRow { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivityAggregationRow>> AggregateByPersonaAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var q = _db.CopilotActivityEvents.Where(e => e.OrganizationId == organizationId && e.Persona != null);
        if (fromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(e => e.OccurredAtUtc < toUtc.Value);

        return await q
            .GroupBy(e => e.Persona!)
            .Select(g => new ActivityAggregationRow { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DailyActivityAggregationRow>> AggregateDailyAsync(
        Guid organizationId,
        int days = 30,
        CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var events = await _db.CopilotActivityEvents
            .Where(e => e.OrganizationId == organizationId && e.OccurredAtUtc >= from)
            .Select(e => e.OccurredAtUtc)
            .ToListAsync(ct);

        return events
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyActivityAggregationRow { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<int> CountDistinctActorsAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var q = _db.CopilotActivityEvents.Where(e => e.OrganizationId == organizationId && e.ActorId != null);
        if (fromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(e => e.OccurredAtUtc < toUtc.Value);

        return await q.Select(e => e.ActorId!).Distinct().CountAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
