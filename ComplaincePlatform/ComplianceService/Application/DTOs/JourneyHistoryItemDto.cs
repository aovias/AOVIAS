namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for a single journey history entry.
/// Excludes SnapshotJson (internal use only) and TenantId (security).
/// </summary>
public class JourneyHistoryItemDto
{
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
    /// Optional remarks/reason for the transition.
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// Timestamp when the transition occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; }
}
