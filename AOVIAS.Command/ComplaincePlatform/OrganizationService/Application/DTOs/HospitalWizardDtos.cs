namespace OrganizationService.Application.DTOs;

/// <summary>
/// Second-step onboarding wizard payload for hospital (internal/Gateway orchestration).
/// </summary>
public class HospitalWizardProfileDto
{
    public Guid OrganizationId { get; set; }
    public string? HospitalType { get; set; }
    public string? BedCapacityRange { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool MarkOnboarded { get; set; } = true;
    public int WizardVersion { get; set; }
    public IReadOnlyList<Guid> DepartmentMasterIds { get; set; } = Array.Empty<Guid>();
}
