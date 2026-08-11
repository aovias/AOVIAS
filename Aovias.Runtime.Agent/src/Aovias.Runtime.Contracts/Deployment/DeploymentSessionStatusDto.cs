namespace Aovias.Runtime.Contracts.Deployment;

/// <summary>
/// Operator-facing deployment session status streamed to the Command UI (Sprint 12.1 D17).
/// </summary>
public sealed class DeploymentSessionStatusDto
{
    public required string DeploymentId { get; init; }

    public required DeploymentSessionState State { get; init; }

    public string? ServiceId { get; init; }

    public string? ReleaseId { get; init; }

    public int OverallProgressPercent { get; init; }

    public string? CurrentPhase { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset UpdatedUtc { get; init; }
}
