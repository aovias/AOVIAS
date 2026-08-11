namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Request to deploy a staged release.
/// </summary>
public sealed class DeployReleaseRequest
{
    public required string ReleaseId { get; init; }

    public required string RequestedBy { get; init; }

    public bool OperatorConfirmed { get; init; }
}
