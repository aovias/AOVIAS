namespace ComplianceService.Domain.Enums;

public enum DeficiencyStatus
{
    Identified       = 0,
    Assigned         = 1,
    InProgress       = 2,
    EvidenceSubmitted = 3,
    ReviewPending    = 4,
    Verified         = 5,
    Closed           = 6,
    Cancelled        = 7,
}
