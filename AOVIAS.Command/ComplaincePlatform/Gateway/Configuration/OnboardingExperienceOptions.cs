namespace Gateway.Configuration;

/// <summary>CRP Phase 3 — welcome experience / communication links.</summary>
public sealed class OnboardingExperienceOptions
{
    public const string SectionName = "OnboardingExperience";

    public string? PublicAppBaseUrl { get; set; }
    public string? LoginPath { get; set; } = "/login";
    public string? DocsBaseUrl { get; set; } = "/docs";
    public string? SupportEmail { get; set; } = "support@aovias.com";
    public bool SendGettingStartedEmail { get; set; } = true;
}
