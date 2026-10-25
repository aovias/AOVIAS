using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests.Journey;

public class OperationalModeProgressionPolicyTests
{
    private readonly OperationalModeProgressionPolicy _policy = new();

    [Fact]
    public void CanChangeOperationalMode_true_when_draft_unlocked()
    {
        var j = new ComplianceJourney(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            12,
            0);

        Assert.True(_policy.CanChangeOperationalMode(j, out var reason));
        Assert.Null(reason);
    }

    [Fact]
    public void CanChangeOperationalMode_false_when_locked_after_submission()
    {
        var j = new ComplianceJourney(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            12,
            0);

        var actor = Guid.NewGuid();
        j.TransitionTo(ComplianceLifecycleState.Enrolled, actor);
        j.TransitionTo(ComplianceLifecycleState.GapAssessment, actor);
        j.TransitionTo(ComplianceLifecycleState.InProgress, actor);
        j.TransitionTo(ComplianceLifecycleState.Submitted, actor);

        Assert.False(_policy.CanChangeOperationalMode(j, out var reason));
        Assert.False(string.IsNullOrEmpty(reason));
    }
}
