using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Metrics;

/// <summary>
/// In-memory metrics snapshot history.
/// </summary>
public sealed class RuntimeMetricsStore : IRuntimeMetricsStore
{
    private readonly object sync = new();
    private readonly LinkedList<RuntimeMetricsSnapshotDto> snapshots = new();
    private readonly IOptionsMonitor<ObservabilitySettings> observabilitySettings;

    public RuntimeMetricsStore(IOptionsMonitor<ObservabilitySettings> observabilitySettings)
    {
        this.observabilitySettings = observabilitySettings;
    }

    /// <inheritdoc />
    public void Add(RuntimeMetricsSnapshotDto snapshot)
    {
        lock (sync)
        {
            snapshots.AddFirst(snapshot);
            var retention = Math.Max(10, observabilitySettings.CurrentValue.SnapshotRetentionCount);
            while (snapshots.Count > retention)
            {
                snapshots.RemoveLast();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RuntimeMetricsSnapshotDto> GetRecent(int limit = 60)
    {
        lock (sync)
        {
            return snapshots.Take(Math.Max(1, limit)).ToList();
        }
    }

    /// <inheritdoc />
    public RuntimeMetricsSnapshotDto? GetLatest()
    {
        lock (sync)
        {
            return snapshots.FirstOrDefault();
        }
    }
}
