namespace ComplianceService.Domain.Enums;

/// <summary>
/// Represents the current state of a hospital's compliance journey for a framework.
/// Framework-specific state machines may use different state transitions.
/// </summary>
public enum JourneyState
{
    /// <summary>
    /// Initial state after framework enrollment
    /// </summary>
    Enrolled = 0,

    /// <summary>
    /// Hospital is actively working on compliance requirements
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Compliance requirements are being reviewed/validated
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Hospital is ready for audit
    /// </summary>
    AuditReady = 3,

    /// <summary>
    /// Audit is in progress
    /// </summary>
    AuditInProgress = 4,

    /// <summary>
    /// Compliance has been certified/accredited
    /// </summary>
    Certified = 5,

    /// <summary>
    /// Compliance certification has expired
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Compliance journey has been suspended
    /// </summary>
    Suspended = 7
}
