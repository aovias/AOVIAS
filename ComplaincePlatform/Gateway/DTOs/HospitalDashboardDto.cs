using System.Collections.Generic;

namespace Gateway.DTOs;

public sealed class HospitalDashboardDto
{
    public string HospitalId { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public decimal RiskIndex { get; set; }
    public List<DepartmentComplianceDto> DepartmentBreakdown { get; set; } = new();
    public List<ChapterComplianceDto> ChapterBreakdown { get; set; } = new();
}

public sealed class DepartmentComplianceDto
{
    public string Department { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
}

public sealed class ChapterComplianceDto
{
    public string ChapterId { get; set; } = string.Empty;
    public string ChapterCode { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
}
