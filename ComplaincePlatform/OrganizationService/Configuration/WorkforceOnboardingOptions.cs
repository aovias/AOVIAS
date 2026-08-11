namespace OrganizationService.Configuration;

public sealed class WorkforceOnboardingOptions
{
    public const string SectionName = "WorkforceOnboarding";

    public int InvitationExpiryDays { get; set; } = 7;
    public string DefaultEmploymentType { get; set; } = "Permanent";
    public bool RequireSeatDefault { get; set; } = true;
    public bool FailSeatIfUnavailable { get; set; } = true;
    public string PublicAppBaseUrl { get; set; } = "http://localhost:4200";
    public string AcceptPath { get; set; } = "/invitations/accept";
}
