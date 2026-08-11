namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Visual rollback plan (Sprint 12 D14).
/// </summary>
public sealed class RollbackPresentationItem
{
    public required string ServiceName { get; init; }

    public required string TargetVersion { get; init; }

    public required string EstimatedDuration { get; init; }

    public required string RiskLevel { get; init; }
}
