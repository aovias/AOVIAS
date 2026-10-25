using System.Collections.Concurrent;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// Caches the single latest <see cref="RuntimeHeartbeatDto"/> received from each environment
/// (Sprint M2.7). Kept separate from <see cref="IFleetRegistry"/> (which only projects a few
/// fields — <c>RuntimeVersion</c>/<c>HealthStatus</c>/<c>MaintenanceMode</c>/<c>LastHeartbeatUtc</c>
/// — onto <see cref="RemoteEnvironment"/>) because Fleet Health/Fleet Drift/Fleet Reconciliation/
/// Fleet Metrics all need the full heartbeat payload (capabilities, desired state version, full
/// reconciliation/deployment summaries), and every one of those Command Center APIs must read only
/// this cache — never call an agent live — to stay fast and tolerant of offline agents (Final
/// Engineering Rule: "Fleet data must tolerate offline agents").
/// </summary>
public interface IFleetHeartbeatCache
{
    void Set(string environmentId, RuntimeHeartbeatDto heartbeat);

    RuntimeHeartbeatDto? Get(string environmentId);

    IReadOnlyDictionary<string, RuntimeHeartbeatDto> GetAll();

    void Remove(string environmentId);
}

public sealed class InMemoryFleetHeartbeatCache : IFleetHeartbeatCache
{
    private readonly ConcurrentDictionary<string, RuntimeHeartbeatDto> heartbeats = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string environmentId, RuntimeHeartbeatDto heartbeat) => heartbeats[environmentId] = heartbeat;

    public RuntimeHeartbeatDto? Get(string environmentId)
        => heartbeats.TryGetValue(environmentId, out var heartbeat) ? heartbeat : null;

    public IReadOnlyDictionary<string, RuntimeHeartbeatDto> GetAll() => heartbeats.ToDictionary(
        pair => pair.Key,
        pair => pair.Value,
        StringComparer.OrdinalIgnoreCase);

    public void Remove(string environmentId) => heartbeats.TryRemove(environmentId, out _);
}
