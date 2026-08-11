namespace ComplianceService.Application.Reporting.Dtos;

public sealed class ComplianceReportDto
{
    public int TotalClauses { get; init; }
    public int CompliantClauses { get; init; }
    public int PartialClauses { get; init; }
    public int NonCompliantClauses { get; init; }
    public decimal ComplianceScore { get; init; }
    public List<ChapterComplianceDto> Chapters { get; init; } = new();
    public List<TrendPointDto> Trends { get; init; } = new();
    public List<RiskChapterDto> TopRiskChapters { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class ChapterComplianceDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public string Status { get; init; } = "non-compliant";
}

public sealed class TrendPointDto
{
    public DateTime Date { get; init; }
    public decimal Value { get; init; }
}

public sealed class RiskChapterDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public string Severity { get; init; } = "medium";
    public string Details { get; init; } = string.Empty;
}
