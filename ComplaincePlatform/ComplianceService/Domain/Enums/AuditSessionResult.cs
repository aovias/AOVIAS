namespace ComplianceService.Domain.Enums;

public enum AuditSessionResult
{
    NotDetermined = 0,
    Compliant = 1,
    ConditionallyCompliant = 2,
    NonCompliant = 3,
    PartiallyCompliant = 4,
}
