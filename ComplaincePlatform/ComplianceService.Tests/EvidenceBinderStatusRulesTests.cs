using ComplianceService.Application.Services;
using Xunit;

namespace ComplianceService.Tests;

public sealed class EvidenceBinderStatusRulesTests
{
    [Fact]
    public void ComputeObjectiveStatus_NoEvidence_IsMissing()
    {
        Assert.Equal(EvidenceBinderStatusRules.Missing, EvidenceBinderStatusRules.ComputeObjectiveStatus(false, false));
    }

    [Fact]
    public void ComputeObjectiveStatus_EvidenceButNoneApproved_IsPartial()
    {
        Assert.Equal(EvidenceBinderStatusRules.Partial, EvidenceBinderStatusRules.ComputeObjectiveStatus(true, false));
    }

    [Fact]
    public void ComputeObjectiveStatus_AnyApproved_IsApproved_IgnoresPending()
    {
        Assert.Equal(EvidenceBinderStatusRules.Approved, EvidenceBinderStatusRules.ComputeObjectiveStatus(true, true));
    }

    [Fact]
    public void ComputeChapterRiskLevel_Missing_IsHigh()
    {
        Assert.Equal("high", EvidenceBinderStatusRules.ComputeChapterRiskLevel(missingCount: 1, partialCount: 0));
    }

    [Fact]
    public void ComputeChapterRiskLevel_OnlyPartial_IsMedium()
    {
        Assert.Equal("medium", EvidenceBinderStatusRules.ComputeChapterRiskLevel(missingCount: 0, partialCount: 2));
    }

    [Fact]
    public void ComputeChapterRiskLevel_None_IsLow()
    {
        Assert.Equal("low", EvidenceBinderStatusRules.ComputeChapterRiskLevel(0, 0));
    }
}
