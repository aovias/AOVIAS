using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class ObligationProjectionRulesTests
{
    [Fact]
    public void ComputeEvidenceStatus_NoLinks_IsMissing()
    {
        var status = ObligationProjectionRules.ComputeEvidenceStatus(Array.Empty<ObjectiveElementEvidence>());
        Assert.Equal(EvidenceStatus.Missing, status);
    }

    [Fact]
    public void ComputeEvidenceStatus_PendingOnly_IsSubmitted()
    {
        var links = new[] { Link(EvidenceVerificationStatus.Pending) };
        Assert.Equal(EvidenceStatus.Submitted, ObligationProjectionRules.ComputeEvidenceStatus(links));
    }

    [Fact]
    public void ComputeEvidenceStatus_MixedPendingAndRejected_IsPartial()
    {
        var links = new[]
        {
            Link(EvidenceVerificationStatus.Pending),
            Link(EvidenceVerificationStatus.Rejected)
        };
        Assert.Equal(EvidenceStatus.Partial, ObligationProjectionRules.ComputeEvidenceStatus(links));
    }

    [Fact]
    public void ComputeEvidenceStatus_AnyVerified_IsVerified()
    {
        var links = new[]
        {
            Link(EvidenceVerificationStatus.Rejected),
            Link(EvidenceVerificationStatus.Verified)
        };
        Assert.Equal(EvidenceStatus.Verified, ObligationProjectionRules.ComputeEvidenceStatus(links));
    }

    [Fact]
    public void ComputeVerificationStatus_NoLinks_IsNotVerified()
    {
        Assert.Equal(
            VerificationStatus.NotVerified,
            ObligationProjectionRules.ComputeVerificationStatus(Array.Empty<ObjectiveElementEvidence>()));
    }

    [Fact]
    public void ComputeVerificationStatus_Pending_IsPending()
    {
        var links = new[] { Link(EvidenceVerificationStatus.Pending) };
        Assert.Equal(VerificationStatus.Pending, ObligationProjectionRules.ComputeVerificationStatus(links));
    }

    [Fact]
    public void ComputeVerificationStatus_VerifiedWinsOverPending()
    {
        var links = new[]
        {
            Link(EvidenceVerificationStatus.Pending),
            Link(EvidenceVerificationStatus.Verified)
        };
        Assert.Equal(VerificationStatus.Verified, ObligationProjectionRules.ComputeVerificationStatus(links));
    }

    [Fact]
    public void ComputeVerificationStatus_RejectedWithoutVerified_IsRejected()
    {
        var links = new[] { Link(EvidenceVerificationStatus.Rejected) };
        Assert.Equal(VerificationStatus.Rejected, ObligationProjectionRules.ComputeVerificationStatus(links));
    }

    [Fact]
    public void ComputeVerificationStatus_NeedsMoreEvidence_IsPending()
    {
        var links = new[] { Link(EvidenceVerificationStatus.NeedsMoreEvidence) };
        Assert.Equal(VerificationStatus.Pending, ObligationProjectionRules.ComputeVerificationStatus(links));
    }

    [Fact]
    public void ComputeEvidenceStatus_SubmittedOnly_IsSubmitted()
    {
        var links = new[] { Link(EvidenceVerificationStatus.Submitted) };
        Assert.Equal(EvidenceStatus.Submitted, ObligationProjectionRules.ComputeEvidenceStatus(links));
    }

    [Fact]
    public void ComputeJourneyProgress_Completed_Is100Percent()
    {
        var journey = new ObjectiveJourney { Status = ObjectiveJourneyStatus.Completed, CurrentStepIndex = 3 };
        var (status, pct) = ObligationProjectionRules.ComputeJourneyProgress(journey);
        Assert.Equal(ObligationJourneyStatus.Completed, status);
        Assert.Equal(100m, pct);
    }

    [Fact]
    public void ReadinessScore_UsesStandardWeights()
    {
        var score = ObligationReadinessCalculator.CalculateObligationReadiness(
            ObligationAssessmentStatus.Passed,
            ImplementationStatus.Implemented,
            EvidenceStatus.Verified,
            VerificationStatus.Verified);

        Assert.Equal(100m, score);
    }

    private static ObjectiveElementEvidence Link(EvidenceVerificationStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            VerificationStatus = status,
            LinkedAt = DateTime.UtcNow
        };
}
