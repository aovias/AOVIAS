namespace ComplianceService.Domain.Enums;

public enum AuditSessionStatus
{
    Planned = 0,
    InProgress = 1,
    Review = 2,
    DraftReport = 3,
    Submitted = 4,
    Closed = 5,
}
