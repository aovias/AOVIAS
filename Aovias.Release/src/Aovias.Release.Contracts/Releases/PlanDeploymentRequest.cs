namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Request to generate a deployment plan.
/// </summary>
public sealed class PlanDeploymentRequest
{
    public required string ReleaseId { get; init; }

    public required string RequestedBy { get; init; }
}
