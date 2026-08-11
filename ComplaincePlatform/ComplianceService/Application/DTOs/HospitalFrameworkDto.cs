namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for hospital framework enrollment information.
/// </summary>
public class HospitalFrameworkDto
{
    public Guid FrameworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
}
