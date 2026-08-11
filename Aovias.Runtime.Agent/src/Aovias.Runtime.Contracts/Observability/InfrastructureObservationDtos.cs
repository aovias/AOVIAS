namespace Aovias.Runtime.Contracts.Observability;

/// <summary>
/// Observed Docker network state.
/// </summary>
public sealed class NetworkObservationDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Driver { get; init; }

    public required string Scope { get; init; }

    public required int ContainerCount { get; init; }
}

/// <summary>
/// Observed Docker volume state.
/// </summary>
public sealed class VolumeObservationDto
{
    public required string Name { get; init; }

    public required string Driver { get; init; }

    public required string Mountpoint { get; init; }
}

/// <summary>
/// Observed release state summary.
/// </summary>
public sealed class ReleaseObservationDto
{
    public required string ReleaseId { get; init; }

    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string State { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Observed workflow execution summary.
/// </summary>
public sealed class WorkflowObservationDto
{
    public required string WorkflowId { get; init; }

    public required string WorkflowName { get; init; }

    public required string WorkflowType { get; init; }

    public required string Status { get; init; }

    public string? CurrentStep { get; init; }

    public int CompletedSteps { get; init; }

    public int TotalSteps { get; init; }

    public string? ReleaseId { get; init; }

    public required DateTimeOffset StartedAt { get; init; }
}
