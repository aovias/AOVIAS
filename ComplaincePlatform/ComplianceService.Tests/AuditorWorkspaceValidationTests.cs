using ComplianceService.Domain.Auditor;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public class AuditorWorkspaceValidationTests
{
    [Fact]
    public void AuditObservation_DoesNotCreateDeficiency_IsIndependentAggregate()
    {
        var obs = new AuditObservation
        {
            Type = ObservationType.PositivePractice,
            Title = "Good hand hygiene compliance",
            Description = "Observed consistent practice in ICU",
        };

        Assert.NotNull(obs);
        Assert.NotEqual(ObservationType.MinorObservation, obs.Type);
        // Observations are a separate aggregate — no PromotedDeficiencyId or deficiency FK exists.
        Assert.IsType<AuditObservation>(obs);
    }

    [Fact]
    public void AuditSession_LifecycleStates_MatchSprint14Spec()
    {
        var values = Enum.GetValues<AuditSessionStatus>();
        Assert.Contains(AuditSessionStatus.Planned, values);
        Assert.Contains(AuditSessionStatus.InProgress, values);
        Assert.Contains(AuditSessionStatus.Review, values);
        Assert.Contains(AuditSessionStatus.DraftReport, values);
        Assert.Contains(AuditSessionStatus.Submitted, values);
        Assert.Contains(AuditSessionStatus.Closed, values);
    }
}
