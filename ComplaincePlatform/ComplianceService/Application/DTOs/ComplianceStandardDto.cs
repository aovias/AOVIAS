namespace ComplianceService.Application.DTOs;

public class ComplianceStandardDto
{
    public Guid Id { get; set; }

    /// <summary>Parent chapter for this standard (NABH hierarchy).</summary>
    public Guid ChapterId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Full standard text when available.</summary>
    public string? Description { get; set; }

    public int Sequence { get; set; }

    public decimal Weight { get; set; }

    public IReadOnlyCollection<ObjectiveElementDto> ObjectiveElements { get; set; } = Array.Empty<ObjectiveElementDto>();

    public decimal? AverageScore { get; set; }

    public decimal? CoreOeAverage { get; set; }

    public int NaCount { get; set; }

    public decimal? CompletionPercent { get; set; }

    public string? RiskBadge { get; set; }

    public string? CoreSummary { get; set; }

    public int? OpenIssuesCount { get; set; }

    public string? ComplianceStatusLabel { get; set; }

    public string? EvidenceGapLabel { get; set; }

    /// <summary>up | down | flat</summary>
    public string? ChangeTrend { get; set; }

    public bool? PreviouslyFailed { get; set; }

    public bool? ExcellenceBadge { get; set; }
}
