namespace ComplianceService.Domain.Enums;

/// <summary>
/// Journey execution progress for an obligation (per-OE wizard execution state).
/// </summary>
public enum ObligationJourneyStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Blocked = 3
}
