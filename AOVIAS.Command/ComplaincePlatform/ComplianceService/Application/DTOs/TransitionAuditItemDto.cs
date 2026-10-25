namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for a single transition in the audit report.
/// Used for cross-journey reporting.
/// </summary>
public class TransitionAuditItemDto
{
    /// <summary>
    /// Compliance journey ID.
    /// </summary>
    public Guid JourneyId { get; init; }

    /// <summary>
    /// Source state before transition.
    /// </summary>
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// Target state after transition.
    /// </summary>
    public string To { get; init; } = string.Empty;

    /// <summary>
    /// User ID who initiated the transition.
    /// </summary>
    public Guid ChangedBy { get; init; }

    /// <summary>
    /// Role of the user who initiated the transition.
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Timestamp when the transition occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; }
}
