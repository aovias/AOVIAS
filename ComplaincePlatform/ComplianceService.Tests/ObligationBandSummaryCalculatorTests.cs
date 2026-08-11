using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class ObligationBandSummaryCalculatorTests
{
    [Fact]
    public void BuildBand_UsesMutuallyExclusiveBucketsThatSumToTotal()
    {
        var obligations = new List<OrganizationComplianceObligation>
        {
            MakeObligation(ObjectiveElementClassification.Core, VerificationStatus.Verified,
                ImplementationStatus.Implemented, ObligationAssessmentStatus.Passed, EvidenceStatus.Verified),
            MakeObligation(ObjectiveElementClassification.Core, VerificationStatus.NotVerified,
                ImplementationStatus.NotImplemented, ObligationAssessmentStatus.Failed, EvidenceStatus.Missing),
            MakeObligation(ObjectiveElementClassification.Core, VerificationStatus.NotVerified,
                ImplementationStatus.Implemented, ObligationAssessmentStatus.Passed, EvidenceStatus.Missing),
            MakeObligation(ObjectiveElementClassification.Core, VerificationStatus.NotVerified,
                ImplementationStatus.PartiallyImplemented, ObligationAssessmentStatus.PartiallyPassed, EvidenceStatus.Partial),
            MakeObligation(ObjectiveElementClassification.Core, VerificationStatus.NotVerified,
                ImplementationStatus.NotStarted, ObligationAssessmentStatus.NotAssessed, EvidenceStatus.Missing),
            MakeObligation(ObjectiveElementClassification.Core, VerificationStatus.NotVerified,
                ImplementationStatus.Unknown, ObligationAssessmentStatus.NotAssessed, EvidenceStatus.Missing),
            MakeObligation(ObjectiveElementClassification.Commitment, VerificationStatus.NotVerified,
                ImplementationStatus.Unknown, ObligationAssessmentStatus.NotAssessed, EvidenceStatus.Missing),
        };

        var core = ObligationBandSummaryCalculator.BuildBand(obligations, ObjectiveElementClassification.Core);

        Assert.Equal(6, core.Total);
        Assert.Equal(1, core.Verified);
        Assert.Equal(1, core.Deficient);
        Assert.Equal(1, core.Implemented);
        Assert.Equal(1, core.Partial);
        Assert.Equal(1, core.NotStarted);
        Assert.Equal(1, core.Unknown);
        Assert.Equal(core.Total, core.Verified + core.Implemented + core.Partial + core.Deficient + core.NotStarted + core.Unknown);
    }

    private static OrganizationComplianceObligation MakeObligation(
        ObjectiveElementClassification band,
        VerificationStatus verification,
        ImplementationStatus implementation,
        ObligationAssessmentStatus assessment,
        EvidenceStatus evidence) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaturityLevel = band,
            VerificationStatus = verification,
            ImplementationStatus = implementation,
            AssessmentStatus = assessment,
            EvidenceStatus = evidence,
            ReadinessScore = 0m,
        };
}
