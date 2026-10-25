namespace Shared.Infrastructure.DTO;

/// <summary>
/// DTO for hospital framework enrollment information.
/// </summary>
public class HospitalFrameworkDto
{
    public Guid FrameworkId { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime EnrolledOn { get; set; }
}
