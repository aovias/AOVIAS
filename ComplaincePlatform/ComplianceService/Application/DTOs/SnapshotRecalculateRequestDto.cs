namespace ComplianceService.Application.DTOs;

/// <summary>
/// Request body for manual snapshot recalculation (admin/debug).
/// </summary>
public class SnapshotRecalculateRequestDto
{
    public Guid HospitalId { get; set; }

    public Guid FrameworkId { get; set; }
}
