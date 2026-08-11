namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for a valid state transition option.
/// </summary>
public class ValidTransitionDto
{
    /// <summary>
    /// Transition code (enum name, e.g., "Enrolled", "Submitted", "Approved")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Submit for Review", "Approve Journey")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
