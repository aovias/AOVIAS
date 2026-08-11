using ComplianceService.Application.ComplianceScoring;

namespace ComplianceService.Application.DTOs;

public class ComplianceScoreReportDto
{
    public Guid HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
    public decimal OverallScore { get; init; }
    public decimal AuditConfidenceScore { get; init; }
    public int MajorCount { get; init; }
    public int MinorCount { get; init; }
    public int CriticalCount { get; init; }
    public List<ChapterScoreDto> Chapters { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
}

public class AuditFindingsReportDto
{
    public Guid HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
    public int TotalFindings { get; init; }
    public int MajorCount { get; init; }
    public int MinorCount { get; init; }
    public int ObservationCount { get; init; }
    public int OFICount { get; init; }
    public int UnresolvedCount { get; init; }
    public DateTime GeneratedAt { get; init; }
}

public class NabhReadinessReportDto
{
    public Guid HospitalId { get; init; }
    public decimal OverallScore { get; init; }
    public decimal RiskIndex { get; init; }
    public int ApprovedJourneys { get; init; }
    public int ExpiringIn90Days { get; init; }
    public int SuspendedJourneys { get; init; }
    public DateTime GeneratedAt { get; init; }
}

// This local DTO previously duplicated ChapterScoreDto; keep the canonical one in ComplianceScoring namespace.
