namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Request to roll back a release.
/// </summary>
public sealed class RollbackReleaseRequest
{
    public required string ReleaseId { get; init; }

    public required string RequestedBy { get; init; }
}
