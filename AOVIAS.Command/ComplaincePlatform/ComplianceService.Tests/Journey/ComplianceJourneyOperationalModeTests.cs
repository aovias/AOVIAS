using ComplianceService.Domain;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests.Journey;

public class ComplianceJourneyOperationalModeTests
{
    [Fact]
    public void SetOperationalMode_advances_one_step()
    {
        var j = CreateJourney();
        Assert.Equal(OperationalMode.SelfAssessment, j.OperationalMode);

        j.SetOperationalMode(OperationalMode.InternalAudit);
        Assert.Equal(OperationalMode.InternalAudit, j.OperationalMode);
    }

    [Fact]
    public void SetOperationalMode_same_mode_is_noop()
    {
        var j = CreateJourney();
        j.SetOperationalMode(OperationalMode.SelfAssessment);
        Assert.Equal(OperationalMode.SelfAssessment, j.OperationalMode);
    }

    [Fact]
    public void SetOperationalMode_skip_throws()
    {
        var j = CreateJourney();
        Assert.Throws<InvalidOperationException>(() => j.SetOperationalMode(OperationalMode.FinalAudit));
    }

    private static ComplianceJourney CreateJourney()
    {
        return new ComplianceJourney(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            validityPeriodInMonths: 12,
            minimumRequiredScore: 0);
    }
}
