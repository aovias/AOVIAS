namespace ComplianceService.Domain.Enums;

/// <summary>
/// Manual root-cause classification for compliance deficiencies (Sprint 6 H5).
/// </summary>
public enum RootCauseCategory
{
    None = 0,
    PolicyMissing = 1,
    EvidenceMissing = 2,
    TrainingMissing = 3,
    ProcessFailure = 4,
    DocumentationIssue = 5,
    VerificationFailure = 6,
    Other = 99
}
