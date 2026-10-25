using Gateway.DTOs;

namespace Gateway.Application.Services;

/// <summary>Pure composition of onboarding journey stages from domain snapshots (no I/O).</summary>
public static class OnboardingJourneyComposer
{
    public const string StageWelcome = "Welcome";
    public const string StageEmailVerified = "EmailVerified";
    public const string StageOrganizationReady = "OrganizationReady";
    public const string StageLicenseActive = "LicenseActive";
    public const string StageHospitalProfile = "HospitalProfile";
    public const string StageFrameworkSelection = "FrameworkSelection";
    public const string StageTeamSetup = "TeamSetup";
    public const string StageAssessment = "ComplianceAssessment";
    public const string StageAiRecommendations = "AiRecommendations";
    public const string StageOperational = "OperationalReadiness";

    public sealed record Snapshot(
        bool OrganizationExists,
        bool EmailVerified,
        bool LicenseOperational,
        bool HasFrameworks,
        bool HasAssessment,
        bool HasTeamInvites,
        bool IsOnboarded,
        string Role);

    public static OnboardingJourneyDto Compose(
        Snapshot snap,
        string organizationName,
        Guid organizationId,
        Guid? hospitalId,
        string? hospitalName,
        string? adminName,
        string? adminEmail,
        OnboardingTrialSummaryDto trial)
    {
        var stages = BuildStages(snap);
        var completedRequired = stages.Count(s => !s.IsOptional && s.Status == "Completed");
        var requiredTotal = stages.Count(s => !s.IsOptional);
        var progress = requiredTotal == 0 ? 0 : (int)Math.Round(100.0 * completedRequired / requiredTotal);

        var current = stages.FirstOrDefault(s => s.Status is "Current" or "Blocked")
                      ?? stages.FirstOrDefault(s => s.Status == "Pending");
        var remaining = stages
            .Where(s => s.Status is "Current" or "Pending" or "Blocked")
            .Sum(s => s.EstimatedMinutes);

        var nextAction = snap.IsOnboarded
            ? "Open dashboard"
            : current == null
                ? "Continue setup"
                : $"Continue: {current.Title}";

        return new OnboardingJourneyDto
        {
            OrganizationId = organizationId,
            OrganizationName = organizationName,
            HospitalId = hospitalId,
            HospitalName = hospitalName,
            IsOnboarded = snap.IsOnboarded,
            Role = snap.Role,
            AdministratorName = adminName,
            AdministratorEmail = adminEmail,
            EmailVerified = snap.EmailVerified,
            ProgressPercent = snap.IsOnboarded ? 100 : Math.Min(99, progress),
            CurrentStageCode = current?.Code,
            NextAction = nextAction,
            EstimatedMinutesRemaining = snap.IsOnboarded ? 0 : remaining,
            Stages = stages,
            Trial = trial,
            QuickActions = BuildQuickActions(snap, current)
        };
    }

    private static List<OnboardingStageDto> BuildStages(Snapshot snap)
    {
        // Hospital profile / frameworks inferred from downstream wizard completion signals until IsOnboarded.
        var profileDone = snap.IsOnboarded || snap.HasFrameworks;
        var frameworksDone = snap.IsOnboarded || snap.HasFrameworks;
        var assessmentDone = snap.IsOnboarded || snap.HasAssessment;
        var teamDone = snap.IsOnboarded || snap.HasTeamInvites;
        var aiDone = snap.IsOnboarded || snap.HasAssessment;

        var defs = new (string Code, string Title, int Minutes, bool Done, bool Optional, string? Owner)[]
        {
            (StageWelcome, "Welcome", 2, true, false, null),
            (StageEmailVerified, "Email verified", 1, snap.EmailVerified, false, "Account owner"),
            (StageOrganizationReady, "Organization ready", 1, snap.OrganizationExists, false, null),
            (StageLicenseActive, "License active", 1, snap.LicenseOperational, false, null),
            (StageHospitalProfile, "Hospital configuration", 5, profileDone, false, "Organization administrator"),
            (StageFrameworkSelection, "Framework selection", 5, frameworksDone, false, "Organization administrator"),
            (StageAssessment, "Compliance assessment", 15, assessmentDone, false, null),
            (StageTeamSetup, "Team setup", 5, teamDone, true, null),
            (StageAiRecommendations, "AI recommendations", 3, aiDone, false, null),
            (StageOperational, "Operational readiness", 2, snap.IsOnboarded, false, "Organization administrator"),
        };

        var list = defs.Select(d => new OnboardingStageDto
        {
            Code = d.Code,
            Title = d.Title,
            EstimatedMinutes = d.Minutes,
            IsOptional = d.Optional,
            OwnerHint = d.Owner,
            Status = d.Done ? "Completed" : (d.Optional ? "Optional" : "Pending")
        }).ToList();

        if (snap.IsOnboarded)
        {
            foreach (var s in list.Where(x => !x.IsOptional))
            {
                s.Status = "Completed";
            }
            return list;
        }

        var foundCurrent = false;
        foreach (var stage in list.Where(s => !s.IsOptional))
        {
            if (stage.Status == "Completed")
            {
                continue;
            }

            if (!foundCurrent)
            {
                stage.Status = CanActOnStage(stage.Code, snap.Role) ? "Current" : "Blocked";
                if (stage.Status == "Blocked" && string.IsNullOrWhiteSpace(stage.OwnerHint))
                {
                    stage.OwnerHint = "Organization administrator";
                }
                foundCurrent = true;
            }
            else
            {
                stage.Status = "Pending";
            }
        }

        return list;
    }

    private static bool CanActOnStage(string code, string role)
    {
        var r = (role ?? string.Empty).ToUpperInvariant();
        var isAdmin = r.Contains("ADMIN", StringComparison.Ordinal)
                      || r is "ORG_ADMIN" or "HOSPITAL_ADMIN" or "PLATFORM_ADMIN";
        if (isAdmin)
        {
            return true;
        }

        return code is StageAssessment or StageAiRecommendations or StageWelcome;
    }

    private static List<OnboardingQuickActionDto> BuildQuickActions(Snapshot snap, OnboardingStageDto? current)
    {
        var actions = new List<OnboardingQuickActionDto>();
        if (snap.IsOnboarded)
        {
            actions.Add(new OnboardingQuickActionDto { Code = "dashboard", Label = "Open dashboard", Href = "/app/dashboard" });
            return actions;
        }

        actions.Add(new OnboardingQuickActionDto
        {
            Code = "continue",
            Label = current == null ? "Continue setup" : $"Continue: {current.Title}",
            Href = "/onboarding"
        });
        actions.Add(new OnboardingQuickActionDto { Code = "invite", Label = "Invite team", Href = "/onboarding" });
        actions.Add(new OnboardingQuickActionDto { Code = "docs", Label = "Documentation", Href = "/docs" });
        return actions;
    }
}
