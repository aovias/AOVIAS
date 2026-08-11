namespace ComplianceService.Application.DTOs;

public class DashboardSummaryDto
{
    public Guid HospitalId { get; init; }
    public decimal ComplianceScore { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int PendingTasks { get; init; }
    public int OpenAuditFindings { get; init; }
    public int UpcomingRenewals { get; init; }
}

public class DepartmentComplianceDto
{
    public string Department { get; init; } = string.Empty;
    public decimal ComplianceScore { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
}

public class ChapterComplianceDto
{
    public Guid ChapterId { get; init; }
    public string ChapterCode { get; init; } = string.Empty;
    public string ChapterName { get; init; } = string.Empty;
    public decimal ComplianceScore { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
}

public class HospitalDashboardDto
{
    public Guid HospitalId { get; init; }
    public decimal ComplianceScore { get; init; }
    public decimal RiskIndex { get; init; }
    public List<DepartmentComplianceDto> DepartmentBreakdown { get; init; } = new();
    public List<ChapterComplianceDto> ChapterBreakdown { get; init; } = new();
}

public class ComplianceTrendPointDto
{
    public DateTime Date { get; init; }
    public decimal ComplianceScore { get; init; }
    public int CompletedTasks { get; init; }
    public int PendingTasks { get; init; }
}

public class ComplianceTrendDto
{
    public Guid HospitalId { get; init; }
    public List<ComplianceTrendPointDto> Points { get; init; } = new();
}

