namespace ComplianceServiceManager.Models;

public sealed class AIFixOptions
{
    public bool DryRun { get; init; }

    public bool AutoApply { get; init; } = true;

    public int MaxRetries { get; init; } = 2;

    public TimeSpan StageTimeout { get; init; } = TimeSpan.FromMinutes(3);

    public bool RequireApprovalForHighRiskChanges { get; init; }
}
