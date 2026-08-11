namespace ComplianceService.Application.DTOs;

/// <summary>
/// Paginated response for transition audit report.
/// </summary>
public class PaginatedTransitionAuditDto
{
    /// <summary>
    /// Total number of transitions matching the filter criteria.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// List of transition audit items for the current page.
    /// </summary>
    public List<TransitionAuditItemDto> Items { get; init; } = new();
}
