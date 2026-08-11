namespace Aovias.Runtime.Contracts.Deployment;

/// <summary>
/// Canonical deployment phase identifiers streamed to operators over SignalR.
/// </summary>
public enum DeploymentPhaseKind
{
    Package,
    Upload,
    Extract,
    DockerLoad,
    Provision,
    Compose,
    Health,
    Smoke,
    Certification,
    Rollback,
    Failure
}

/// <summary>
/// Real-time deployment phase event for the Deployment Command Center.
/// </summary>
public sealed class DeploymentPhaseEventDto
{
    public required string DeploymentId { get; init; }

    public string? ServiceId { get; init; }

    public string? ReleaseId { get; init; }

    public string? WorkflowId { get; init; }

    public DeploymentPhaseKind Phase { get; init; }

    public required string Status { get; init; }

    public required string Message { get; init; }

    public int ProgressPercent { get; init; }

    public int OverallProgressPercent { get; init; }

    public string? FailureCategory { get; init; }

    public string? RecoveryAction { get; init; }

    public string? DocumentationHint { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string? Component { get; init; }

    public string? Operation { get; init; }

    public string? ExceptionType { get; init; }

    public string? InnerExceptionMessage { get; init; }
}
