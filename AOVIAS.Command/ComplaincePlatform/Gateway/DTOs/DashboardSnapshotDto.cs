using System;
using System.Collections.Generic;

namespace Gateway.DTOs;

public sealed class DashboardSnapshotDto
{
    public bool IsSnapshotAvailable { get; set; }
    public bool SnapshotMaterialized { get; set; }
    public Guid? ComplianceJourneyId { get; set; }
    public Guid? FrameworkId { get; set; }
    public decimal OverallScore { get; set; }
    public decimal AuditConfidenceScore { get; set; }
    public DateTime? CalculatedAt { get; set; }

    public Guid? TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string GeneratedAt { get; set; } = string.Empty;
    public SnapshotRangeDto SnapshotRange { get; set; } = new();
    public DashboardSnapshotSummaryDto Summary { get; set; } = new();
    public List<FrameworkSnapshotDto> Frameworks { get; set; } = new();
    public List<HospitalSnapshotDto> Hospitals { get; set; } = new();
    public List<ScoreTrendDto> ScoreTrends { get; set; } = new();
    public TasksSummaryDto TasksSummary { get; set; } = new();
    public List<MilestoneSnapshotDto> Milestones { get; set; } = new();
    public List<AlertSnapshotDto> Alerts { get; set; } = new();
    public SnapshotFiltersDto Filters { get; set; } = new();
}

public sealed class SnapshotRangeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public sealed class DashboardSnapshotSummaryDto
{
    public int TotalHospitals { get; set; }
    public int OnboardedHospitals { get; set; }
    public int TotalJourneys { get; set; }
    public int ActiveJourneys { get; set; }
    public int UpcomingExpiries { get; set; }
    public int OverdueTasks { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AvgComplianceScore { get; set; }
    public int FrameworksActive { get; set; }
}

public sealed class FrameworkSnapshotDto
{
    public string FrameworkCode { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public int ActiveHospitals { get; set; }
    public int Journeys { get; set; }
    public decimal AvgScore { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public List<MilestoneSnapshotDto> UpcomingMilestones { get; set; } = new();
    public List<ActivitySnapshotDto> RecentActivity { get; set; } = new();
}

public sealed class ActivitySnapshotDto
{
    public string ActivityId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string FrameworkCode { get; set; } = string.Empty;
}

public sealed class HospitalSnapshotDto
{
    public string HospitalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsOnboarded { get; set; }
    public string? NabhLevel { get; set; }
    public int OpenTasks { get; set; }
    public int OverdueTasks { get; set; }
    public string LastUpdated { get; set; } = string.Empty;
    public List<JourneySnapshotDto> Journeys { get; set; } = new();
}

public sealed class JourneySnapshotDto
{
    public string JourneyId { get; set; } = string.Empty;
    public string FrameworkCode { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal ProgressPercent { get; set; }
}

public sealed class ScoreTrendDto
{
    public string Date { get; set; } = string.Empty;
    public decimal AvgScore { get; set; }
    public Dictionary<string, decimal> Frameworks { get; set; } = new();
}

public sealed class TasksSummaryDto
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int Overdue { get; set; }
    public int DueSoon { get; set; }
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
    public List<TasksByFrameworkDto> ByFramework { get; set; } = new();
}

public sealed class TasksByFrameworkDto
{
    public string FrameworkCode { get; set; } = string.Empty;
    public int Open { get; set; }
    public int Overdue { get; set; }
}

public sealed class MilestoneSnapshotDto
{
    public string MilestoneId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? FrameworkCode { get; set; }
    public string? Owner { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class AlertSnapshotDto
{
    public string AlertId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public bool Acknowledged { get; set; }
}

public sealed class SnapshotFiltersDto
{
    public List<string> Frameworks { get; set; } = new();
    public List<FilterHospitalDto> Hospitals { get; set; } = new();
    public SnapshotRangeDto DateRange { get; set; } = new();
}

public sealed class FilterHospitalDto
{
    public string HospitalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
