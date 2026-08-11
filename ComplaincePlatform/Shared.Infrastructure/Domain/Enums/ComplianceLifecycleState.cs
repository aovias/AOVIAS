namespace Shared.Infrastructure.Domain.Enums;

/// <summary>
/// Core Compliance Lifecycle states that must exist for every framework.
/// Represents the standard state machine for compliance journey across all frameworks.
/// </summary>
public enum ComplianceLifecycleState
{
    /// <summary>
    /// Initial draft state - framework enrollment is being prepared
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Hospital has been enrolled in the framework
    /// </summary>
    Enrolled = 1,

    /// <summary>
    /// Gap assessment is being conducted to identify compliance gaps
    /// </summary>
    GapAssessment = 2,

    /// <summary>
    /// Hospital is actively working on compliance requirements
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Compliance documentation has been submitted for review
    /// </summary>
    Submitted = 4,

    /// <summary>
    /// Audit has been scheduled
    /// </summary>
    AuditScheduled = 5,

    /// <summary>
    /// Audit has been completed
    /// </summary>
    AuditCompleted = 6,

    /// <summary>
    /// Compliance has been approved/certified
    /// </summary>
    Approved = 7,

    /// <summary>
    /// Compliance submission has been rejected
    /// </summary>
    Rejected = 8,

    /// <summary>
    /// Compliance certification has expired
    /// </summary>
    Expired = 9,

    /// <summary>
    /// Renewal process is in progress
    /// </summary>
    RenewalInProgress = 10,

    /// <summary>
    /// Compliance has been renewed
    /// </summary>
    Renewed = 11,

    /// <summary>
    /// Compliance journey has been suspended (conditional state)
    /// </summary>
    Suspended = 12
}
