namespace Gateway.Configuration;

/// <summary>
/// Feature flags for second-step onboarding (see docs/organization-member-invitations.md).
/// </summary>
public sealed class OnboardingWizardOptions
{
    public const string SectionName = "OnboardingWizard";

    /// <summary>
    /// When true (CRP Phase 7 default), wizard creates email invitations instead of eager Identity register.
    /// Set false only for rollback to the legacy register path.
    /// </summary>
    public bool UseEmailInvites { get; set; } = true;
}
