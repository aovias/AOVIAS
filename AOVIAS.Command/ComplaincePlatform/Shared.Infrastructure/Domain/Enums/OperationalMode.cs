namespace Shared.Infrastructure.Domain.Enums;

/// <summary>
/// Operational phase of compliance execution (e.g. NABH self-assessment vs audit vs surveillance).
/// This is distinct from <see cref="ComplianceLifecycleState"/>, which governs the compliance journey workflow
/// (Draft, Submitted, Approved, etc.).
/// </summary>
public enum OperationalMode
{
    SelfAssessment = 0,
    InternalAudit = 1,
    FinalAudit = 2,
    Surveillance = 3,
    ReAccreditation = 4
}
