namespace ComplianceService.Application.ComplianceScoring;

/// <summary>
/// Data transfer object representing the compliance score for a chapter.
/// </summary>
public class ChapterScoreDto
{
    /// <summary>
    /// Unique identifier of the compliance chapter.
    /// </summary>
    public Guid ChapterId { get; set; }

    /// <summary>
    /// Weighted score for this chapter (0-100).
    /// </summary>
    public decimal WeightedScore { get; set; }

    /// <summary>
    /// Completion percentage for this chapter (0-100).
    /// </summary>
    public decimal CompletionPercentage { get; set; }

    /// <summary>
    /// Risk penalty applied to this chapter's score (0-100).
    /// </summary>
    public decimal RiskPenalty { get; set; }
}
