namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO representing program overview snapshot data from ComplianceSnapshot.
/// </summary>
public class ProgramOverviewSnapshotDto
{
    /// <summary>
    /// When false, scores cannot be resolved (no journey for hospital/framework).
    /// When true, scores are either from a persisted snapshot row or computed live from the active journey.
    /// </summary>
    public bool IsSnapshotAvailable { get; set; }

    /// <summary>
    /// True when values come from <c>ComplianceSnapshots</c>; false when computed on read from OE/journey (materialization queued).
    /// </summary>
    public bool SnapshotMaterialized { get; set; }

    /// <summary>
    /// Journey this snapshot is derived from, when available.
    /// </summary>
    public Guid? ComplianceJourneyId { get; set; }

    /// <summary>
    /// Framework identifier for this snapshot.
    /// </summary>
    public Guid? FrameworkId { get; set; }

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
    /// Count of observations from audit simulation.
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
    /// JSON representation of chapter-level score breakdown.
    /// Contains serialized list of ChapterScoreDto objects.
    /// </summary>
    public string ChapterBreakdownJson { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this snapshot was calculated (null when <see cref="IsSnapshotAvailable"/> is false).
    /// </summary>
    public DateTime? CalculatedAt { get; set; }
}
