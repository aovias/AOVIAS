namespace ComplianceService.Domain;

/// <summary>
/// Persisted onboarding wizard questionnaire (hospital-level, framework-agnostic).
/// </summary>
public class WizardOnboardingAssessment
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public int WizardVersion { get; set; }
    public string AnswersJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
}
