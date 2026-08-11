using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Stateless utility for computing obligation readiness scores.
/// Formula: (Assessment × 0.30) + (Implementation × 0.30) + (Evidence × 0.20) + (Verification × 0.20)
/// </summary>
public static class ObligationReadinessCalculator
{
    public static decimal CalculateObligationReadiness(
        ObligationAssessmentStatus assessment,
        ImplementationStatus implementation,
        EvidenceStatus evidence,
        VerificationStatus verification)
    {
        var a = ScoreAssessment(assessment);
        var i = ScoreImplementation(implementation);
        var e = ScoreEvidence(evidence);
        var v = ScoreVerification(verification);

        return Math.Round((a * 0.30m) + (i * 0.30m) + (e * 0.20m) + (v * 0.20m), 2);
    }

    public static decimal CalculateStageReadiness(
        IEnumerable<OrganizationComplianceObligation> obligations,
        ObjectiveElementClassification stage)
    {
        var stageObs = obligations
            .Where(o => o.MaturityLevel == stage)
            .ToList();

        return AverageReadiness(stageObs);
    }

    public static decimal CalculateJourneyReadiness(IEnumerable<OrganizationComplianceObligation> obligations)
    {
        return AverageReadiness(obligations.ToList());
    }

    private static decimal AverageReadiness(List<OrganizationComplianceObligation> list)
    {
        if (list.Count == 0) return 0m;

        var total = list.Sum(o =>
            CalculateObligationReadiness(o.AssessmentStatus, o.ImplementationStatus, o.EvidenceStatus, o.VerificationStatus));

        return Math.Round(total / list.Count, 2);
    }

    private static decimal ScoreAssessment(ObligationAssessmentStatus s) => s switch
    {
        ObligationAssessmentStatus.Passed => 100m,
        ObligationAssessmentStatus.PartiallyPassed => 50m,
        _ => 0m
    };

    private static decimal ScoreImplementation(ImplementationStatus s) => s switch
    {
        ImplementationStatus.Implemented => 100m,
        ImplementationStatus.PartiallyImplemented => 50m,
        _ => 0m
    };

    private static decimal ScoreEvidence(EvidenceStatus s) => s switch
    {
        EvidenceStatus.Verified => 100m,
        EvidenceStatus.Submitted => 75m,
        EvidenceStatus.Partial => 50m,
        _ => 0m
    };

    private static decimal ScoreVerification(VerificationStatus s) => s switch
    {
        VerificationStatus.Verified => 100m,
        VerificationStatus.Pending => 50m,
        _ => 0m
    };
}
