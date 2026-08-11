using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.StateStore;

/// <summary>
/// Thread-safe singleton runtime state store.
/// </summary>
public sealed class RuntimeStateStore : IRuntimeStateStore
{
    private readonly object sync = new();
    private RuntimeStateDto snapshot = CreateInitial();

    /// <inheritdoc />
    public RuntimeStateDto GetSnapshot()
    {
        lock (sync)
        {
            return snapshot;
        }
    }

    /// <inheritdoc />
    public void UpdateSnapshot(RuntimeStateDto updatedSnapshot)
    {
        ArgumentNullException.ThrowIfNull(updatedSnapshot);
        lock (sync)
        {
            snapshot = updatedSnapshot;
        }
    }

    internal static RuntimeStateDto CreateInitial()
        => new()
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            RuntimeStatus = "Starting",
            HealthStatus = "Unknown",
            HealthMessage = "Observability platform initializing.",
            CpuUsagePercent = 0,
            MemoryUsagePercent = 0,
            DiskUsagePercent = 0,
            DockerStatus = "Unknown",
            ContainerCount = 0,
            RunningContainerCount = 0,
            NetworkCount = 0,
            VolumeCount = 0,
            ImageCount = 0,
            RunningWorkflowCount = 0,
            DeploymentQueueCount = 0,
            ActiveReleaseCount = 0,
            Capabilities = [],
            Containers = [],
            Networks = [],
            Volumes = [],
            Releases = [],
            Workflows = []
        };
}
