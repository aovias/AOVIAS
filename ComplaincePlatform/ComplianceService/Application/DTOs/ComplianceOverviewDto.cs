namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for organization-level compliance overview.
/// Enterprise-wide aggregated metrics across all hospitals and frameworks.
/// </summary>
public class ComplianceOverviewDto
{
    /// <summary>
    /// Percentage of journeys in Approved state.
    /// Calculated as: (Approved / Total Journeys) * 100
    /// </summary>
    public decimal PercentageApproved { get; init; }

    /// <summary>
    /// Number of journeys with RiskScore between 40 and 70.
    /// </summary>
    public int AtRiskCount { get; init; }

    /// <summary>
    /// Number of journeys with critical risk indicators:
    /// - RiskScore > 70
    /// - OR Status == Suspended
    /// - OR Status == Expired
    /// </summary>
    public int CriticalCount { get; init; }

    /// <summary>
    /// Expiry heatmap grouped by month (Year-Month format).
    /// Shows count of Approved journeys expiring in each month.
    /// Includes next 12 months only.
    /// </summary>
    public List<ExpiryHeatmapItemDto> ExpiryHeatmap { get; init; } = new();
}

/// <summary>
/// DTO for a single month in the expiry heatmap.
/// </summary>
public class ExpiryHeatmapItemDto
{
    /// <summary>
    /// Month in Year-Month format (e.g., "2026-03").
    /// </summary>
    public string Month { get; init; } = string.Empty;

    /// <summary>
    /// Number of Approved journeys expiring in this month.
    /// </summary>
    public int Count { get; init; }
}
