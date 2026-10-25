using ComplianceService.Application.EvidenceReview;
using ComplianceService.Domain;
using Xunit;

namespace ComplianceService.Tests;

public sealed class EvidenceVerificationStateMachineTests
{
    [Theory]
    [InlineData(EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.Verified, true)]
    [InlineData(EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.Rejected, true)]
    [InlineData(EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.NeedsMoreEvidence, true)]
    [InlineData(EvidenceVerificationStatus.Verified, EvidenceVerificationStatus.Rejected, false)]
    [InlineData(EvidenceVerificationStatus.Rejected, EvidenceVerificationStatus.Pending, true)]
    [InlineData(EvidenceVerificationStatus.NeedsMoreEvidence, EvidenceVerificationStatus.Submitted, true)]
    public void CanTransition_ReturnsExpected(
        EvidenceVerificationStatus from,
        EvidenceVerificationStatus to,
        bool expected)
    {
        Assert.Equal(expected, EvidenceVerificationStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void MapReviewAction_Approve_MapsToVerified()
    {
        Assert.Equal(
            EvidenceVerificationStatus.Verified,
            EvidenceVerificationStateMachine.MapReviewAction(EvidenceReviewDecision.Approve));
    }

    [Fact]
    public void BlocksTaskCompletion_ForRejectedAndNeedsMore()
    {
        Assert.True(EvidenceVerificationStateMachine.BlocksTaskCompletion(EvidenceVerificationStatus.Rejected));
        Assert.True(EvidenceVerificationStateMachine.BlocksTaskCompletion(EvidenceVerificationStatus.NeedsMoreEvidence));
        Assert.False(EvidenceVerificationStateMachine.BlocksTaskCompletion(EvidenceVerificationStatus.Verified));
    }

    [Fact]
    public void Verified_IsTerminal_BlocksDoubleApprove()
    {
        Assert.False(EvidenceVerificationStateMachine.CanTransition(
            EvidenceVerificationStatus.Verified,
            EvidenceVerificationStatus.Verified));
        Assert.False(EvidenceVerificationStateMachine.CanTransition(
            EvidenceVerificationStatus.Verified,
            EvidenceVerificationStatus.Pending));
        Assert.True(EvidenceVerificationStateMachine.IsApproved(EvidenceVerificationStatus.Verified));
    }

    [Theory]
    [InlineData(EvidenceVerificationStatus.Draft, EvidenceVerificationStatus.Submitted, true)]
    [InlineData(EvidenceVerificationStatus.Submitted, EvidenceVerificationStatus.Pending, true)]
    [InlineData(EvidenceVerificationStatus.Pending, EvidenceVerificationStatus.Verified, true)]
    public void Smoke_HappyPathTransitions(EvidenceVerificationStatus from, EvidenceVerificationStatus to, bool expected)
    {
        Assert.Equal(expected, EvidenceVerificationStateMachine.CanTransition(from, to));
    }
}
