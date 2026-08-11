namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Grouped deployment history row (Sprint 12 D13).
/// </summary>
public sealed class DeploymentHistoryGroupItem
{
    public required string PeriodLabel { get; init; }

    public required string ServiceName { get; init; }

    public required string Outcome { get; init; }

    public required string DeploymentId { get; init; }

    public string Summary => $"{ServiceName} — {Outcome}";
}
