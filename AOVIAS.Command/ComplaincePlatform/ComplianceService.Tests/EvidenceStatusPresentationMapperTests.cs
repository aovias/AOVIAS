using ComplianceService.Application.Presentation;
using ComplianceService.Domain;
using Xunit;

namespace ComplianceService.Tests;

public sealed class EvidenceStatusPresentationMapperTests
{
    [Theory]
    [InlineData(EvidenceVerificationStatus.Verified, "approved")]
    [InlineData(EvidenceVerificationStatus.Pending, "pendingReview")]
    [InlineData(EvidenceVerificationStatus.NeedsMoreEvidence, "needsMoreEvidence")]
    public void ToApiKey_MatchesStateMachine(EvidenceVerificationStatus status, string expected)
    {
        Assert.Equal(expected, EvidenceStatusPresentationMapper.ToApiKey(status));
    }

    [Theory]
    [InlineData("verified", EvidenceStatusPresentationContext.EvidenceItem, "Approved")]
    [InlineData("approved", EvidenceStatusPresentationContext.EvidenceItem, "Approved")]
    [InlineData("verified", EvidenceStatusPresentationContext.Obligation, "Verified")]
    [InlineData("pendingReview", EvidenceStatusPresentationContext.EvidenceItem, "Pending Review")]
    [InlineData("returnForRevision", EvidenceStatusPresentationContext.EvidenceItem, "Needs More Evidence")]
    public void ToDisplayLabel_NormalizesAliases(string raw, EvidenceStatusPresentationContext context, string expected)
    {
        Assert.Equal(expected, EvidenceStatusPresentationMapper.ToDisplayLabel(raw, context));
    }
}
