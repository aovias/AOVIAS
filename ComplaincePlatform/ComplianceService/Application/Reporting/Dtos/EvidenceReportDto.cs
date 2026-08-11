namespace ComplianceService.Application.Reporting.Dtos;

public sealed class EvidenceReportDto
{
    public int ApprovedEvidence { get; init; }
    public int PendingReview { get; init; }
    public int RejectedEvidence { get; init; }
    public decimal CoverageRatio { get; init; }
    public List<ChapterEvidenceDto> ChapterEvidence { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class ChapterEvidenceDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public int Approved { get; init; }
    public int Pending { get; init; }
    public int Total { get; init; }
}
