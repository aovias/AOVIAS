namespace ComplianceService.Application.Reporting.Dtos;

public sealed class ReadinessReportDto
{
    public decimal ReadinessScore { get; init; }
    public List<HighRiskChapterDto> HighRiskChapters { get; init; } = new();
    public List<MissingEvidenceDto> MissingEvidenceClauses { get; init; } = new();
    public List<RecommendationDto> Recommendations { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class HighRiskChapterDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public string Level { get; init; } = "medium";
    public string Details { get; init; } = string.Empty;
}

public sealed class MissingEvidenceDto
{
    public Guid ObjectiveElementId { get; init; }
    public string ClauseCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class RecommendationDto
{
    public string Text { get; init; } = string.Empty;
    public string Priority { get; init; } = "medium";
}
