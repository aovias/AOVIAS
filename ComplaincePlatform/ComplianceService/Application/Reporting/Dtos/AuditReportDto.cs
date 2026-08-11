namespace ComplianceService.Application.Reporting.Dtos;

public sealed class AuditReportDto
{
    public int CriticalFindings { get; init; }
    public int MajorFindings { get; init; }
    public int MinorFindings { get; init; }
    public int TotalFindings { get; init; }
    public List<ChapterFindingDto> FindingsByChapter { get; init; } = new();
    public List<DepartmentFindingDto> FindingsByDepartment { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class ChapterFindingDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public int Critical { get; init; }
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Total { get; init; }
}

public sealed class DepartmentFindingDto
{
    public string Department { get; init; } = string.Empty;
    public int Count { get; init; }
}
