namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for organization-level regulatory overview used by Gateway.
/// Matches the shape expected by Gateway.DTOs.RegulatoryOverviewDto.
/// </summary>
public class RegulatoryOverviewDto
{
    /// <summary>
    /// Number of active accreditations for the organization.
    /// Typically counts journeys that are Enrolled or Approved.
    /// </summary>
    public int ActiveAccreditations { get; init; }

    /// <summary>
    /// Number of accreditations that are within the renewal window (e.g., next 90 days).
    /// </summary>
    public int UpcomingRenewals { get; init; }

    /// <summary>
    /// Aggregated risk index details including classification and trend.
    /// </summary>
    public RegulatoryRiskIndexDto RiskIndex { get; init; } = new();

    /// <summary>
    /// Primary jurisdiction (country/region) for the organization.
    /// </summary>
    public RegulatoryJurisdictionDto PrimaryJurisdiction { get; init; } = new();
}

/// <summary>
/// Risk index details including score, classification and trend.
/// </summary>
public class RegulatoryRiskIndexDto
{
    public int Score { get; init; }
    public string Classification { get; init; } = string.Empty;
    public string Trend { get; init; } = string.Empty;
    public int Delta { get; init; }
    public DateTime LastAssessedAt { get; init; }
}

/// <summary>
/// Jurisdiction information for an organization.
/// </summary>
public class RegulatoryJurisdictionDto
{
    public string Country { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
}

