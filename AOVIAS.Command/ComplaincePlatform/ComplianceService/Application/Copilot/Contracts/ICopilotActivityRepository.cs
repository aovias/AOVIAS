using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

public interface ICopilotActivityRepository
{
    Task RecordAsync(CopilotActivityEvent activity, CancellationToken ct = default);

    Task<IReadOnlyList<CopilotActivityEvent>> GetRecentAsync(
        Guid organizationId,
        int limit = 50,
        CancellationToken ct = default);

    Task<IReadOnlyList<CopilotActivityEvent>> GetByDateRangeAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? eventType = null,
        string? persona = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<ActivityAggregationRow>> AggregateByEventTypeAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task<IReadOnlyList<ActivityAggregationRow>> AggregateByPersonaAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task<IReadOnlyList<DailyActivityAggregationRow>> AggregateDailyAsync(
        Guid organizationId,
        int days = 30,
        CancellationToken ct = default);

    Task<int> CountDistinctActorsAsync(
        Guid organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed class ActivityAggregationRow
{
    public string Key { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class DailyActivityAggregationRow
{
    public DateOnly Date { get; init; }
    public int Count { get; init; }
}
