using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Level 2 Snapshot: Framework-level compliance snapshot for a hospital.
/// Represents compliance posture for a specific framework at a point in time.
/// </summary>
public class ComplianceSnapshot : BaseEntity
{
    /// <summary>
    /// Optional tenant/organization identifier for multi-tenant isolation.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Unique identifier of the hospital this snapshot belongs to.
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Framework identifier this snapshot is for.
    /// </summary>
    public Guid FrameworkId { get; set; }

    /// <summary>
    /// Compliance journey this snapshot was derived from (read model root).
    /// </summary>
    public Guid? ComplianceJourneyId { get; set; }

    /// <summary>
    /// Overall compliance score (0-100).
    /// </summary>
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Audit confidence score indicating readiness for audit (0-100).
    /// </summary>
    public decimal AuditConfidenceScore { get; set; }

    /// <summary>
    /// Count of critical non-conformances.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// Count of major non-conformances.
    /// </summary>
    public int MajorCount { get; set; }

    /// <summary>
    /// Count of minor non-conformances.
    /// </summary>
    public int MinorCount { get; set; }

    /// <summary>
    /// JSON representation of chapter-level score breakdown.
    /// Contains serialized list of ChapterScoreDto objects.
    /// </summary>
    public string ChapterBreakdownJson { get; set; } = string.Empty;

    /// <summary>
    /// Count of Major Non-Conformances from audit simulation.
    /// </summary>
    public int MajorNCCount { get; set; }

    /// <summary>
    /// Count of Minor Non-Conformances from audit simulation.
    /// </summary>
    public int MinorNCCount { get; set; }

    /// <summary>
    /// Count of Observations from audit simulation.
    /// </summary>
    public int ObservationCount { get; set; }

    /// <summary>
    /// Count of Opportunities For Improvement from audit simulation.
    /// </summary>
    public int OFICount { get; set; }

    /// <summary>
    /// JSON representation of high-risk chapters from audit simulation.
    /// Contains serialized list of HighRiskChapter objects.
    /// </summary>
    public string HighRiskChaptersJson { get; set; } = string.Empty;

    /// <summary>
    /// Count of critical risks across all chapters.
    /// </summary>
    public int CriticalRiskCount { get; set; }

    /// <summary>
    /// Current journey state of the compliance framework.
    /// </summary>
    public JourneyState JourneyState { get; set; }

    /// <summary>
    /// Timestamp when this snapshot was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}
