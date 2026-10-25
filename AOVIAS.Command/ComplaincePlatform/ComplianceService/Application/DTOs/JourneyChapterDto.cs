namespace ComplianceService.Application.DTOs;

/// <summary>
/// DTO for a compliance chapter within a journey.
/// </summary>
public class JourneyChapterDto
{
    public Guid ChapterId { get; set; }
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public int Sequence { get; set; }
    
    /// <summary>
    /// Completion percentage (0-100)
    /// </summary>
    public decimal CompletionPercentage { get; set; }
    
    /// <summary>
    /// Total number of mandatory objective elements in this chapter.
    /// </summary>
    public int TotalObjectiveElements { get; set; }
    
    /// <summary>
    /// Number of objective elements with verified evidence linkage.
    /// </summary>
    public int CompletedObjectiveElements { get; set; }

    public decimal? AverageScore { get; set; }
    public int FailedCoreOeCount { get; set; }
		public string? Risk { get; set; }
		public string? CoreComplianceStatus { get; set; }
		public int? OpenIssuesCount { get; set; }
		public bool? PendingAudit { get; set; }
		public bool? CoreFailureFlag { get; set; }
}
