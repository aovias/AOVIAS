namespace Gateway.DTOs;

/// <summary>
/// DTO for hospital-level lifecycle summary.
/// Aggregated statistics across all compliance journeys for a hospital.
/// </summary>
public class JourneySummaryDto
{
    /// <summary>
    /// Total number of compliance frameworks the hospital is enrolled in.
    /// </summary>
    public int TotalFrameworks { get; init; }

    /// <summary>
    /// Number of journeys in Approved state.
    /// </summary>
    public int Approved { get; init; }

    /// <summary>
    /// Number of Approved journeys expiring within the renewal window (typically 90 days).
    /// Uses FrameworkConfiguration.RenewalDaysBeforeExpiry for each framework.
    /// </summary>
    public int ExpiringIn90Days { get; init; }

    /// <summary>
    /// Number of journeys in Suspended state.
    /// </summary>
    public int Suspended { get; init; }

    /// <summary>
    /// Aggregated risk index across all journeys.
    /// Calculated as: AVG(RiskScore) with weighted severity:
    /// - Critical (>70): weight 3
    /// - AtRisk (40-70): weight 2
    /// - Safe (<40): weight 1
    /// </summary>
    public decimal RiskIndex { get; init; }
}
