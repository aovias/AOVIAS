namespace ComplianceService.Domain;

/// <summary>
/// Result of an audit simulation containing non-conformance counts and high-risk chapters.
/// </summary>
public class AuditSimulationResult
{
    /// <summary>
    /// Count of Major Non-Conformances detected.
    /// </summary>
    public int MajorNCCount { get; set; }

    /// <summary>
    /// Count of Minor Non-Conformances detected.
    /// </summary>
    public int MinorNCCount { get; set; }

    /// <summary>
    /// Count of Observations detected.
    /// </summary>
    public int ObservationCount { get; set; }

    /// <summary>
    /// Count of Opportunities For Improvement detected.
    /// </summary>
    public int OFICount { get; set; }

    /// <summary>
    /// List of chapters flagged as High Risk (completion < 50%).
    /// </summary>
    public List<HighRiskChapter> HighRiskChapters { get; set; } = new();

    /// <summary>
    /// Total count of all non-conformances.
    /// </summary>
    public int TotalNonConformances => MajorNCCount + MinorNCCount + ObservationCount + OFICount;
}

/// <summary>
/// Represents a chapter flagged as high risk due to low completion percentage.
/// </summary>
public class HighRiskChapter
{
    /// <summary>
    /// The chapter ID.
    /// </summary>
    public Guid ChapterId { get; set; }

    /// <summary>
    /// The chapter code (e.g., PRE, ICC, HRM).
    /// </summary>
    public string ChapterCode { get; set; } = string.Empty;

    /// <summary>
    /// The chapter name.
    /// </summary>
    public string ChapterName { get; set; } = string.Empty;

    /// <summary>
    /// Completion percentage (0-100).
    /// </summary>
    public decimal CompletionPercentage { get; set; }

    /// <summary>
    /// Total number of objective elements in the chapter.
    /// </summary>
    public int TotalObjectiveElements { get; set; }

    /// <summary>
    /// Number of completed objective elements.
    /// </summary>
    public int CompletedObjectiveElements { get; set; }
}
