namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Architecture-only rollback result.
/// </summary>
public sealed class RollbackReleaseResult
{
    public required string ReleaseId { get; init; }

    public required string State { get; init; }

    public required string Message { get; init; }

    public required bool QueuedForExecution { get; init; }
}
