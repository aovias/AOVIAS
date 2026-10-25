namespace ComplianceService.Domain.Enums;

/// <summary>
/// Persistence status for OE execution wizard progress (distinct from compliance journey lifecycle).
/// </summary>
public enum ObjectiveJourneyStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2
}
