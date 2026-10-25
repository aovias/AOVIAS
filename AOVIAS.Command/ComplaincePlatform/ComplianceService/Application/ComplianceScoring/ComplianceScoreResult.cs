namespace ComplianceService.Application.ComplianceScoring;

/// <summary>
/// Data transfer object representing the overall compliance score result for a hospital.
/// </summary>
public class ComplianceScoreResult
{
    /// <summary>
    /// Unique identifier of the hospital.
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Overall compliance score (0-100).
    /// </summary>
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Audit confidence score indicating readiness for audit (0-100).
    /// </summary>
    public decimal AuditConfidenceScore { get; set; }

    /// <summary>
    /// Count of major non-conformances.
    /// </summary>
    public int MajorCount { get; set; }

    /// <summary>
    /// Count of minor non-conformances.
    /// </summary>
    public int MinorCount { get; set; }

    /// <summary>
    /// Count of critical non-conformances.
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// List of chapter-level scores.
    /// </summary>
    public List<ChapterScoreDto> ChapterScores { get; set; } = new();

    /// <summary>
    /// Count of observations identified (if available from scoring/audit simulation).
    /// </summary>
    public int ObservationsCount { get; set; }

    /// <summary>
    /// Count of opportunities for improvement (OFI) identified (if available).
    /// </summary>
    public int OFICount { get; set; }

    /// <summary>
    /// Optional enrichment keyed by Objective Element id (Standard / classification context).
    /// </summary>
    public Dictionary<Guid, ObjectiveElementContextDto> ObjectiveElementContexts { get; set; } = new();
}
