namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Architecture-only deployment result returned before real execution exists.
/// </summary>
public sealed class DeployReleaseResult
{
    public required string ReleaseId { get; init; }

    public required string State { get; init; }

    public required string Message { get; init; }

    public required bool QueuedForExecution { get; init; }

    public string? WorkflowId { get; init; }
}
