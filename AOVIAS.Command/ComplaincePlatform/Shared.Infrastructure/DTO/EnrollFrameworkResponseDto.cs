namespace Shared.Infrastructure.DTO;

/// <summary>
/// Response DTO for framework enrollment.
/// </summary>
public class EnrollFrameworkResponseDto
{
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledOn { get; set; }
}
