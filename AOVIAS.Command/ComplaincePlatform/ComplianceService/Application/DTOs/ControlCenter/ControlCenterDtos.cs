using System;
using System.Collections.Generic;

namespace ComplianceService.Application.DTOs.ControlCenter;

/// <summary>
/// Aggregate snapshot for the Compliance Command Center page (used by initial load and the
/// 30-second auto-refresh). Each section can also be fetched independently via granular endpoints.
/// </summary>
public sealed class ControlCenterOverviewDto
{
    public ComplianceHeaderDto Header { get; set; } = new();
    public ComplianceIntelligenceDto Intelligence { get; set; } = new();
    public List<RecommendedActionDto> RecommendedActions { get; set; } = new();
    public ComplianceCommandCenterKpiDto Kpis { get; set; } = new();
    public List<HighRiskChapterDto> HighRiskChapters { get; set; } = new();
    public List<DepartmentRiskDto> DepartmentRisks { get; set; } = new();
    public List<ActionCenterItemDto> ActionCenter { get; set; } = new();
    public List<ComplianceTrendPointDto> Trend { get; set; } = new();
    public List<ChapterComplianceDto> ChapterCompliance { get; set; } = new();
    public EvidenceCoverageDto EvidenceCoverage { get; set; } = new();
    public AuditReadinessDto AuditReadiness { get; set; } = new();
    public List<ActivityEventDto> RecentActivity { get; set; } = new();
    public List<AlertDto> Alerts { get; set; } = new();
}

// MODULE 1 - Header
public sealed class ComplianceHeaderDto
{
    public bool IsLive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

// MODULE 2 - AI Compliance Intelligence
public sealed class ComplianceIntelligenceDto
{
    public List<ComplianceInsightDto> Insights { get; set; } = new();
}

public sealed class ComplianceInsightDto
{
    /// <summary>Insight kind: ComplianceDrop, ComplianceImprovement, MissingEvidence, CriticalTasks, AuditRisk, OverdueCapa, PolicyGap, DocumentExpiry.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>UI severity bucket: critical | warning | success | info.</summary>
    public string Severity { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string? Metric { get; set; }
}

// MODULE 3 - Recommended Actions
public sealed class RecommendedActionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>high | medium | low.</summary>
    public string Priority { get; set; } = "medium";
    public decimal ImpactPercent { get; set; }
    /// <summary>UploadEvidence, CompleteTask, ApproveCAPA, ReviewAuditFinding, UpdatePolicy, AssignOwner.</summary>
    public string ActionType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
}

// MODULE 4 - Executive KPI cards
public sealed class ComplianceCommandCenterKpiDto
{
    public decimal ComplianceReadiness { get; set; }
    public decimal AuditConfidence { get; set; }
    public int OpenFindings { get; set; }
    public int CriticalTasks { get; set; }
    public decimal EvidenceCoverage { get; set; }
    public int UpcomingAudits { get; set; }

    // Trend deltas (signed). Percentages for the % cards, counts for the count cards.
    public decimal ComplianceReadinessTrend { get; set; }
    public decimal AuditConfidenceTrend { get; set; }
    public int OpenFindingsTrend { get; set; }
    public int CriticalTasksTrend { get; set; }
    public decimal EvidenceCoverageTrend { get; set; }
    public int UpcomingAuditsTrend { get; set; }

    // Optional sparkline series (chronological) for the % cards; empty when no history.
    public List<decimal> ComplianceReadinessSparkline { get; set; } = new();
    public List<decimal> AuditConfidenceSparkline { get; set; } = new();
    public List<decimal> EvidenceCoverageSparkline { get; set; } = new();
}

// MODULE 5 - Risk Command Center
public sealed class HighRiskChapterDto
{
    public string Chapter { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    /// <summary>low | medium | high.</summary>
    public string RiskLevel { get; set; } = "low";
    public int OpenIssues { get; set; }
}

public sealed class DepartmentRiskDto
{
    public string Department { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    /// <summary>low | medium | high.</summary>
    public string RiskLevel { get; set; } = "low";
}

// MODULE 6 - Action Center
public sealed class ActionCenterItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ComplianceImpact { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    /// <summary>critical | high | medium.</summary>
    public string Priority { get; set; } = "medium";
    /// <summary>overdue | due-soon | on-track.</summary>
    public string Status { get; set; } = "on-track";
}

// MODULE 7 - Compliance Analytics
public sealed class ComplianceTrendPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Compliance { get; set; }
    public decimal Target { get; set; }
}

public sealed class ChapterComplianceDto
{
    public string Chapter { get; set; } = string.Empty;
    public decimal Compliance { get; set; }
}

public sealed class EvidenceCoverageDto
{
    public int Approved { get; set; }
    public int Pending { get; set; }
    public int Rejected { get; set; }
    public int Total { get; set; }
    public decimal CoveragePercent { get; set; }
}

// MODULE 8 - Audit Readiness
public sealed class AuditReadinessDto
{
    public decimal ConfidenceScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CriticalFindings { get; set; }
    public int CorrectiveActionsPending { get; set; }
    public int ClausesMissingEvidence { get; set; }
    public decimal AuditPassProbability { get; set; }
    public decimal ThresholdPercent { get; set; } = 80m;
    public string Recommendation { get; set; } = string.Empty;
}

// MODULE 9 - Activity Stream
public sealed class ActivityEventDto
{
    public string Id { get; set; } = string.Empty;
    /// <summary>task | evidence | audit | finding (mapped to UI tabs).</summary>
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Department { get; set; }
}

// MODULE 10 - Smart Alerts
public sealed class AlertDto
{
    public string Id { get; set; } = string.Empty;
    /// <summary>critical | warning | info (UI buckets).</summary>
    public string Severity { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<AlertActionDto> Actions { get; set; } = new();
}

public sealed class AlertActionDto
{
    public string Label { get; set; } = string.Empty;
    /// <summary>primary | secondary.</summary>
    public string Type { get; set; } = "primary";
    public string Action { get; set; } = string.Empty;
}
