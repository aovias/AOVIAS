namespace ComplianceService.Application.DTOs;

/// <summary>
/// Response DTO for framework enrollment.
/// </summary>
public class EnrollmentResponseDto
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}
