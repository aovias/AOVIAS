using Gateway.Application.Services;
using Gateway.DTOs;
using Xunit;

namespace Gateway.UnitTests;

public sealed class OnboardingJourneyComposerTests
{
    [Fact]
    public void Compose_marks_operational_complete_when_onboarded()
    {
        var snap = new OnboardingJourneyComposer.Snapshot(
            OrganizationExists: true,
            EmailVerified: true,
            LicenseOperational: true,
            HasFrameworks: true,
            HasAssessment: true,
            HasTeamInvites: true,
            IsOnboarded: true,
            Role: "ORG_ADMIN");

        var dto = OnboardingJourneyComposer.Compose(
            snap,
            "Acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Acme Hospital",
            "Ada Admin",
            "ada@acme.test",
            new OnboardingTrialSummaryDto { IsOperational = true, MaxSeats = 10, UsedSeats = 1, DaysRemaining = 28 });

        Assert.Equal(100, dto.ProgressPercent);
        Assert.Equal(0, dto.EstimatedMinutesRemaining);
        Assert.All(dto.Stages.Where(s => !s.IsOptional), s => Assert.Equal("Completed", s.Status));
        Assert.Equal("Open dashboard", dto.NextAction);
    }

    [Fact]
    public void Compose_sets_current_stage_and_progress_for_new_org()
    {
        var snap = new OnboardingJourneyComposer.Snapshot(
            OrganizationExists: true,
            EmailVerified: false,
            LicenseOperational: true,
            HasFrameworks: false,
            HasAssessment: false,
            HasTeamInvites: false,
            IsOnboarded: false,
            Role: "ORG_ADMIN");

        var dto = OnboardingJourneyComposer.Compose(
            snap,
            "Acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Acme Hospital",
            "Ada",
            "ada@acme.test",
            new OnboardingTrialSummaryDto { IsOperational = true, MaxSeats = 10, UsedSeats = 1, DaysRemaining = 30 });

        Assert.True(dto.ProgressPercent is > 0 and < 100);
        Assert.Contains(dto.Stages, s => s.Status == "Current");
        Assert.True(dto.EstimatedMinutesRemaining > 0);
        Assert.StartsWith("Continue:", dto.NextAction);
    }

    [Fact]
    public void Compose_blocks_admin_gates_for_auditor()
    {
        var snap = new OnboardingJourneyComposer.Snapshot(
            OrganizationExists: true,
            EmailVerified: true,
            LicenseOperational: true,
            HasFrameworks: false,
            HasAssessment: false,
            HasTeamInvites: false,
            IsOnboarded: false,
            Role: "AUDITOR");

        var dto = OnboardingJourneyComposer.Compose(
            snap,
            "Acme",
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            new OnboardingTrialSummaryDto { IsOperational = true });

        var current = dto.Stages.First(s => s.Status is "Current" or "Blocked");
        Assert.Equal("Blocked", current.Status);
        Assert.False(string.IsNullOrWhiteSpace(current.OwnerHint));
    }
}
