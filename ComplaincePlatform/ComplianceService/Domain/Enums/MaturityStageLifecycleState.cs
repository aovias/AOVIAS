namespace ComplianceService.Domain.Enums;

/// <summary>
/// Lifecycle state for a PRAMAN maturity stage (Core → Commitment → Achievement → Excellence).
/// </summary>
public enum MaturityStageLifecycleState
{
    Locked = 0,
    Available = 1,
    InProgress = 2,
    Completed = 3
}
