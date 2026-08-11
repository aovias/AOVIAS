namespace ComplianceService.Application.DTOs;

/// <summary>
/// Acknowledgement that snapshot recalculation was queued (non-blocking).
/// </summary>
public class SnapshotRecalculateResponseDto
{
    public bool Queued { get; set; }
}
