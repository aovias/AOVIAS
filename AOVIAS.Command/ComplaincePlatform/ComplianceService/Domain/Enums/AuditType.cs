namespace ComplianceService.Domain.Enums;

/// <summary>
/// Audit session / schedule type. Commercial labels: Internal, Department, Follow-up, Mock NABH, Focused.
/// Legacy NABH assessment / surveillance values retained for existing sessions.
/// </summary>
public enum AuditType
{
    InternalAudit = 0,
    /// <summary>Mock NABH (commercial label).</summary>
    MockAudit = 1,
    ConsultantReview = 2,
    NabhPreAssessment = 3,
    NabhFinalAssessment = 4,
    Surveillance = 5,
    FocusedAudit = 6,
    DepartmentAudit = 7,
    FollowUpAudit = 8,
}
