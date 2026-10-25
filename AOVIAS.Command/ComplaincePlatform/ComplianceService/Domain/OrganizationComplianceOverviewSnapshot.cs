using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Level 1 Snapshot: Organization-wide compliance overview.
/// Aggregates data across all frameworks for a hospital.
/// Must never query live aggregates - only reads from Framework snapshots.
/// </summary>
public class OrganizationComplianceOverviewSnapshot : BaseEntity
{
    /// <summary>
    /// Unique identifier of the hospital.
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Weighted average score across all frameworks.
    /// </summary>
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Framework ID with the lowest performance score.
    /// </summary>
    public Guid? WorstFrameworkId { get; set; }

    /// <summary>
    /// Total count of critical risks across all frameworks.
    /// </summary>
    public int TotalCriticalRisks { get; set; }

    /// <summary>
    /// JSON array of upcoming renewals with framework IDs and dates.
    /// Format: [{"FrameworkId": "...", "RenewalDate": "...", "DaysUntil": 30}]
    /// </summary>
    public string UpcomingRenewals { get; set; } = "[]";

    /// <summary>
    /// Timestamp when this snapshot was calculated.
    /// </summary>
    public DateTime LastCalculatedAt { get; set; }
}
