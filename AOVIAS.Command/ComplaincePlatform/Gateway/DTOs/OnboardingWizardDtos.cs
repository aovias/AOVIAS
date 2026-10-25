namespace Gateway.DTOs;

public sealed class HospitalOnboardingProfileDto
{
    public Guid HospitalId { get; set; }
    public string? HospitalType { get; set; }

    /// <summary>UI bed-count bucket (e.g. "101-200"); stored as BedCapacityRange in OrganizationService.</summary>
    public string? BedCapacity { get; set; }

    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public List<Guid> DepartmentIds { get; set; } = new();
}

public sealed class WizardAssessmentAnswerClientDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// OE Code (e.g. "AAC.1.a") parsed from the question's title prefix by the frontend.
    /// Forwarded to ComplianceService to resolve the cross-service GUID boundary.
    /// </summary>
    public string? ObjectiveElementCode { get; set; }
}

public sealed class TeamInviteClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// Second-step onboarding wizard payload (authenticated).
/// </summary>
public sealed class OnboardWizardRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public HospitalOnboardingProfileDto HospitalProfile { get; set; } = new();
    public List<string> FrameworkCodes { get; set; } = new();
    public List<WizardAssessmentAnswerClientDto> AssessmentAnswers { get; set; } = new();
    public List<TeamInviteClientDto> TeamInvites { get; set; } = new();
    public int WizardVersion { get; set; }
}

public sealed class OnboardWizardResponseDto
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public IReadOnlyList<string> FrameworkCodesCompleted { get; set; } = Array.Empty<string>();
    /// <summary>Primary KB assessment id (first selected framework).</summary>
    public Guid? AssessmentId { get; set; }
    public IReadOnlyList<Guid>? AssessmentIds { get; set; }
    public OnboardingAssessmentSummaryDto? AssessmentSummary { get; set; }
    public IReadOnlyList<Guid> InvitedUserIds { get; set; } = Array.Empty<Guid>();
}

public sealed class OnboardingAssessmentSummaryDto
{
    // ── Legacy 1–5 scale path fields (preserved; populated by from-wizard endpoint) ──
    public decimal OverallPercent { get; set; }
    public bool MeetsOverallPercent { get; set; }
    public bool CoreRulePassed { get; set; }
    public IReadOnlyList<Guid> CoreFailureNodeIds { get; set; } = Array.Empty<Guid>();
    public bool ChapterAveragesPassed { get; set; }
    public bool StandardAveragesPassed { get; set; }
    public bool CommitmentLevelPassed { get; set; }
    public bool TooManyLowScoresInStandard { get; set; }

    // ── Core maturity path fields (populated by from-core-wizard endpoint) ──
    public decimal? CoreReadinessScore   { get; set; }
    public string?  CoreStatus           { get; set; }  // "Pass" | "Fail"
    public int      CriticalFailures     { get; set; }
    public bool     CanUnlockCommitment  { get; set; }
}
