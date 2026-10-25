using Aovias.Release.Contracts.Enums;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Summary entry for release history views.
/// </summary>
public sealed class ReleaseHistoryEntry
{
    public required string ReleaseId { get; init; }

    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }

    public required ReleaseState State { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public string? Summary { get; init; }

    public string? DeploymentId { get; init; }

    public string? Operator { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public TimeSpan? Duration { get; init; }

    public bool WasRollback { get; init; }

    public string? CorrelationId { get; init; }
}
