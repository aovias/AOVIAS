namespace Aovias.Runtime.Contracts.Observability;

/// <summary>
/// Point-in-time runtime metrics snapshot.
/// </summary>
public sealed class RuntimeMetricsSnapshotDto
{
    public required string SnapshotId { get; init; }

    public required DateTimeOffset CapturedAt { get; init; }

    public required double CpuUsagePercent { get; init; }

    public required double MemoryUsagePercent { get; init; }

    public required double DiskUsagePercent { get; init; }

    public required int ContainerCount { get; init; }

    public required int RunningContainerCount { get; init; }

    public required int NetworkCount { get; init; }

    public required int VolumeCount { get; init; }

    public required int ImageCount { get; init; }

    public required int RunningWorkflowCount { get; init; }

    public required int DeploymentQueueCount { get; init; }
}
