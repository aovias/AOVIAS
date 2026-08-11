namespace Gateway.DTOs;

public sealed class OnboardingJourneyDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid? HospitalId { get; set; }
    public string? HospitalName { get; set; }
    public bool IsOnboarded { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? AdministratorName { get; set; }
    public string? AdministratorEmail { get; set; }
    public bool EmailVerified { get; set; }
    public int ProgressPercent { get; set; }
    public string? CurrentStageCode { get; set; }
    public string? NextAction { get; set; }
    public int EstimatedMinutesRemaining { get; set; }
    public List<OnboardingStageDto> Stages { get; set; } = new();
    public OnboardingTrialSummaryDto Trial { get; set; } = new();
    public List<OnboardingQuickActionDto> QuickActions { get; set; } = new();
}

public sealed class OnboardingStageDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    /// <summary>Completed | Current | Pending | Blocked | Optional</summary>
    public string Status { get; set; } = "Pending";
    public int EstimatedMinutes { get; set; }
    public string? OwnerHint { get; set; }
    public bool IsOptional { get; set; }
}

public sealed class OnboardingTrialSummaryDto
{
    public Guid? LicenseKeyId { get; set; }
    public string? LicenseType { get; set; }
    public string? LicenseStatus { get; set; }
    public int? DaysRemaining { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int MaxSeats { get; set; }
    public int UsedSeats { get; set; }
    public IReadOnlyList<string> Entitlements { get; set; } = Array.Empty<string>();
    public bool IsOperational { get; set; }
}

public sealed class OnboardingQuickActionDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Href { get; set; }
}

public sealed class OnboardingTelemetryEventRequestDto
{
    public string EventType { get; set; } = string.Empty;
    public string? Stage { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string? PayloadJson { get; set; }
}
