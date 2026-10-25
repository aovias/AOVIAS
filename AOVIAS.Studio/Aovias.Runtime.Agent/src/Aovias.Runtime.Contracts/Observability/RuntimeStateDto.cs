namespace Aovias.Runtime.Contracts.Observability;

/// <summary>
/// Canonical runtime state snapshot served by the observability platform.
/// </summary>
public sealed class RuntimeStateDto
{
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string RuntimeStatus { get; init; }

    public required string HealthStatus { get; init; }

    public required string HealthMessage { get; init; }

    public required double CpuUsagePercent { get; init; }

    public required double MemoryUsagePercent { get; init; }

    public required double DiskUsagePercent { get; init; }

    public required string DockerStatus { get; init; }

    public required int ContainerCount { get; init; }

    public required int RunningContainerCount { get; init; }

    public required int NetworkCount { get; init; }

    public required int VolumeCount { get; init; }

    public required int ImageCount { get; init; }

    public required int RunningWorkflowCount { get; init; }

    public required int DeploymentQueueCount { get; init; }

    public required int ActiveReleaseCount { get; init; }

    public required IReadOnlyList<string> Capabilities { get; init; }

    public required IReadOnlyList<ContainerObservationDto> Containers { get; init; }

    public required IReadOnlyList<NetworkObservationDto> Networks { get; init; }

    public required IReadOnlyList<VolumeObservationDto> Volumes { get; init; }

    public required IReadOnlyList<ReleaseObservationDto> Releases { get; init; }

    public required IReadOnlyList<WorkflowObservationDto> Workflows { get; init; }
}
