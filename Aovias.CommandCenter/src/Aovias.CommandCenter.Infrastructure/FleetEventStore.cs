using System.Collections.Concurrent;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// Persists <see cref="FleetEvent"/>s (Sprint M2.7 D-item "Fleet events") — the fleet-scale
/// counterpart of Sprint M2.6's per-agent <c>IReconciliationHistoryStore</c>/<c>ReconciliationEvent</c>
/// discipline, applied here at Command Center scope instead of one Runtime Agent.
/// </summary>
public interface IFleetEventStore
{
    void Append(FleetEvent fleetEvent);

    IReadOnlyList<FleetEvent> GetRecent(string? environmentId = null, int limit = 100);
}

/// <summary>In-memory, bounded (last 2000 events) <see cref="IFleetEventStore"/> — a safe default matching the non-durable convention every other first-cut store in this codebase uses (Sprint M2.7 Technical Debt: no durable/file-backed implementation yet, unlike Sprint M2.6's <c>FileReconciliationHistoryStore</c>).</summary>
public sealed class InMemoryFleetEventStore : IFleetEventStore
{
    private const int MaxEvents = 2000;
    private readonly ConcurrentQueue<FleetEvent> events = new();

    public void Append(FleetEvent fleetEvent)
    {
        ArgumentNullException.ThrowIfNull(fleetEvent);
        events.Enqueue(fleetEvent);

        while (events.Count > MaxEvents && events.TryDequeue(out _))
        {
            // Trim oldest entries beyond the bound.
        }
    }

    public IReadOnlyList<FleetEvent> GetRecent(string? environmentId = null, int limit = 100)
    {
        IEnumerable<FleetEvent> query = events;
        if (environmentId is not null)
        {
            query = query.Where(fleetEvent => string.Equals(fleetEvent.EnvironmentId, environmentId, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(fleetEvent => fleetEvent.TimestampUtc)
            .Take(limit)
            .ToList();
    }
}
