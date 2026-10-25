using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class DeficiencyLifecycleTransitionsTests
{
    [Theory]
    [InlineData(DeficiencyStatus.Identified, DeficiencyLifecycleStatus.Identified)]
    [InlineData(DeficiencyStatus.EvidenceSubmitted, DeficiencyLifecycleStatus.AwaitingEvidence)]
    [InlineData(DeficiencyStatus.ReviewPending, DeficiencyLifecycleStatus.VerificationPending)]
    [InlineData(DeficiencyStatus.Verified, DeficiencyLifecycleStatus.Verified)]
    public void ToLifecycle_maps_persisted_status(DeficiencyStatus status, DeficiencyLifecycleStatus expected)
    {
        Assert.Equal(expected, DeficiencyLifecycleTransitions.ToLifecycle(status));
    }

    [Theory]
    [InlineData(DeficiencyStatus.Identified, DeficiencyStatus.Assigned, true)]
    [InlineData(DeficiencyStatus.Assigned, DeficiencyStatus.InProgress, true)]
    [InlineData(DeficiencyStatus.Closed, DeficiencyStatus.InProgress, false)]
    [InlineData(DeficiencyStatus.InProgress, DeficiencyStatus.EvidenceSubmitted, true)]
    public void CanTransition_enforces_monotonic_progression(
        DeficiencyStatus current,
        DeficiencyStatus target,
        bool expected)
    {
        Assert.Equal(expected, DeficiencyLifecycleTransitions.CanTransition(current, target));
    }

    [Theory]
    [InlineData(1, 1, DeficiencyRiskLevel.Low)]
    [InlineData(3, 3, DeficiencyRiskLevel.Medium)]
    [InlineData(4, 4, DeficiencyRiskLevel.High)]
    [InlineData(5, 5, DeficiencyRiskLevel.Critical)]
    public void ComputeRiskLevel_uses_likelihood_times_impact(int likelihood, int impact, DeficiencyRiskLevel expected)
    {
        Assert.Equal(expected, DeficiencyLifecycleTransitions.ComputeRiskLevel(likelihood, impact));
    }
}
