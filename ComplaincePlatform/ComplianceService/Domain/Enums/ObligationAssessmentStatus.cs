namespace ComplianceService.Domain.Enums;

/// <summary>
/// Assessment outcome for an organization's compliance obligation.
/// Distinct from <see cref="ComplianceEvaluationStatus"/> (audit findings) to avoid coupling.
/// </summary>
public enum ObligationAssessmentStatus
{
    NotAssessed = 0,
    Passed = 1,
    PartiallyPassed = 2,
    Failed = 3
}
