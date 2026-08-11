namespace ComplianceService.Domain.Enums;

/// <summary>
/// Readiness state of evidence for an obligation.
/// </summary>
public enum EvidenceStatus
{
    Missing = 0,
    Partial = 1,
    Submitted = 2,
    Verified = 3
}
