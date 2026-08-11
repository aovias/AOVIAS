namespace ComplianceService.Domain.Enums;

public enum IncidentStatus
{
    Draft = 1,
    Reported = 2,
    UnderInvestigation = 3,
    RcaInProgress = 4,
    PendingReview = 5,
    Closed = 6,
    Cancelled = 7,
}
