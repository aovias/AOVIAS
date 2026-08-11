namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>What happened to one <see cref="RepairTask"/> during one reconciliation cycle (Sprint M2.6 D9).</summary>
public sealed class RepairDecisionRecord
{
    public required string TaskId { get; init; }

    public required RepairActionType ActionType { get; init; }

    public required PolicyDecision Decision { get; init; }

    public required bool Executed { get; init; }

    public bool? Succeeded { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// One complete, durable, replayable record of a reconciliation cycle (Sprint M2.6 D9) — the unit
/// <see cref="IReconciliationHistoryStore"/> persists. Captures everything the brief names:
/// timestamp, observed drift, repair decision, execution result, operator approval, duration, and
/// correlation id.
/// </summary>
public sealed class ReconciliationHistoryRecord
{
    public required string CorrelationId { get; init; }

    public required string EnvironmentId { get; init; }

    public required string ServiceId { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required int DriftFindingCount { get; init; }

    public required IReadOnlyDictionary<DriftSeverity, int> DriftBySeverity { get; init; }

    public required IReadOnlyList<RepairDecisionRecord> RepairDecisions { get; init; }

    public required int OperatorApprovalsRequested { get; init; }

    public required double DurationMs { get; init; }
}

/// <summary>
/// Persists <see cref="ReconciliationHistoryRecord"/>s to provide a complete audit trail (Sprint
/// M2.6 D9). The default, in-memory implementation (<see cref="InMemoryReconciliationHistoryStore"/>)
/// is a safe test double; the durable, file-backed implementation
/// (<c>FileReconciliationHistoryStore</c>) lives in <c>Aovias.Runtime.Agent.Infrastructure.Reconciliation</c>,
/// following the same atomic-write convention <c>RuntimeManifestStore</c> established in earlier
/// sprints.
/// </summary>
public interface IReconciliationHistoryStore
{
    Task AppendAsync(ReconciliationHistoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReconciliationHistoryRecord>> GetHistoryAsync(string? serviceId = null, int limit = 100, CancellationToken cancellationToken = default);
}

/// <summary>An in-memory, non-durable <see cref="IReconciliationHistoryStore"/> — a safe default and test double, not appropriate as the only store in a real deployment (Sprint M2.6 Technical Debt: see the durable file-backed implementation Runtime Agent registers instead).</summary>
public sealed class InMemoryReconciliationHistoryStore : IReconciliationHistoryStore
{
    private readonly List<ReconciliationHistoryRecord> records = [];
    private readonly object gate = new();

    public Task AppendAsync(ReconciliationHistoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (gate)
        {
            records.Add(record);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReconciliationHistoryRecord>> GetHistoryAsync(string? serviceId = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IEnumerable<ReconciliationHistoryRecord> query = records;
            if (serviceId is not null)
            {
                query = query.Where(record => string.Equals(record.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));
            }

            IReadOnlyList<ReconciliationHistoryRecord> result = query
                .OrderByDescending(record => record.TimestampUtc)
                .Take(limit)
                .ToList();

            return Task.FromResult(result);
        }
    }
}
