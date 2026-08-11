using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.DTOs;

// ── Shared ────────────────────────────────────────────────────────────────────

public enum AnalyticsTrendGranularity
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
}

public sealed class AnalyticsDeepLinkDto
{
    public string Label { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> QueryParams { get; init; }
        = new Dictionary<string, string>();
}

public sealed class AnalyticsMetricCardDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string? Unit { get; init; }
    public string? Source { get; init; }
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

public sealed class AnalyticsBreakdownItemDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

public sealed class TrendSeriesDto
{
    public string MetricKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public AnalyticsTrendGranularity Granularity { get; init; }
    public IReadOnlyList<TrendPointDto> Points { get; init; } = Array.Empty<TrendPointDto>();
}

public sealed class AnalyticsContextDto
{
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid FrameworkId { get; init; }
    public string? FrameworkName { get; init; }
    public DateTime GeneratedAt { get; init; }
}

// ── Overview ──────────────────────────────────────────────────────────────────

public sealed class AnalyticsOverviewDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public decimal ReadinessScore { get; init; }
    public decimal ComplianceMaturityIndex { get; init; }
    public int OpenDeficiencies { get; init; }
    public int CriticalDeficiencies { get; init; }
    public double CapaEffectivenessRate { get; init; }
    public double SlaHealthScore { get; init; }
    public double EscalationRate { get; init; }
    public int RecurringIssues { get; init; }
    public int PendingReviews { get; init; }
    public string CurrentStage { get; init; } = string.Empty;
    public IReadOnlyList<AnalyticsMetricCardDto> KpiCards { get; init; } = Array.Empty<AnalyticsMetricCardDto>();
}

// ── Executive ─────────────────────────────────────────────────────────────────

public sealed class ExecutiveAnalyticsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public AnalyticsReadinessSectionDto ComplianceReadiness { get; init; } = new();
    public AnalyticsReadinessSectionDto AccreditationReadiness { get; init; } = new();
    public IReadOnlyList<AnalyticsBreakdownItemDto> DepartmentPerformance { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public CapaGovernanceSummaryDto CapaPerformance { get; init; } = new();
    public SlaGovernanceSummaryDto SlaHealth { get; init; } = new();
    public AnalyticsRiskExposureDto RiskExposure { get; init; } = new();
    public AnalyticsEscalationExposureDto EscalationExposure { get; init; } = new();
}

public sealed class AnalyticsReadinessSectionDto
{
    public decimal Score { get; init; }
    public string Source { get; init; } = string.Empty;
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

public sealed class AnalyticsRiskExposureDto
{
    public int CriticalOpenDeficiencies { get; init; }
    public int HighRiskDeficiencies { get; init; }
    public int CriticalRiskCount { get; init; }
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

public sealed class AnalyticsEscalationExposureDto
{
    public int OpenEscalations { get; init; }
    public int CriticalEscalations { get; init; }
    public double EscalationRate { get; init; }
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

// ── Operational ───────────────────────────────────────────────────────────────

public sealed class OperationalAnalyticsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public AnalyticsDeficiencyFunnelDto DeficiencyFunnel { get; init; } = new();
    public TaskSummaryDto TaskThroughput { get; init; } = new();
    public EvidenceSummaryDto EvidencePipeline { get; init; } = new();
    public int VerificationBacklog { get; init; }
    public int AssignmentLoad { get; init; }
    public int RecurringIssues { get; init; }
    public IReadOnlyList<AnalyticsBreakdownItemDto> RootCauseTrend { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public IReadOnlyList<AnalyticsDeepLinkDto> DeepLinks { get; init; } = Array.Empty<AnalyticsDeepLinkDto>();

    /// <summary>Phase 2 Sprint 6 — CAPA aging from <c>CapaQueryService</c> metrics.</summary>
    public CapaAgingBucketsDto CapaAging { get; init; } = new();
    public double AverageOpenCapaAgeDays { get; init; }
    public double AverageResolutionHours { get; init; }
    public double AverageResolutionDays { get; init; }
    public double AuditCompletionRate { get; init; }
    public int AuditSessionsTotal { get; init; }
    public int AuditSessionsClosedOrSubmitted { get; init; }
    public IReadOnlyList<AnalyticsBreakdownItemDto> DepartmentPerformance { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public double SlaComplianceRate { get; init; }
    public double SlaHealthScore { get; init; }
    public OperationalOverdueWorkDto OverdueWork { get; init; } = new();
}

public sealed class OperationalOverdueWorkDto
{
    public int OverdueCapas { get; init; }
    public int OverdueTasks { get; init; }
    public int OverdueDeficiencies { get; init; }
    public IReadOnlyList<AnalyticsBreakdownItemDto> DepartmentOverdueCapas { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
}

public sealed class AnalyticsDeficiencyFunnelDto
{
    public int Open { get; init; }
    public int AwaitingAssignment { get; init; }
    public int InProgress { get; init; }
    public int AwaitingEvidence { get; init; }
    public int VerificationPending { get; init; }
    public int Verified { get; init; }
    public int Closed { get; init; }
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

// ── RCA ───────────────────────────────────────────────────────────────────────

public sealed class RcaAnalyticsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public IReadOnlyList<AnalyticsBreakdownItemDto> TopRootCauses { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public IReadOnlyList<AnalyticsBreakdownItemDto> RootCauseByDepartment { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public IReadOnlyList<AnalyticsBreakdownItemDto> RootCauseByChapter { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public IReadOnlyList<AnalyticsBreakdownItemDto> RootCauseByStage { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public IReadOnlyList<TrendPointDto> RootCauseTrend { get; init; } = Array.Empty<TrendPointDto>();
    public IReadOnlyList<TrendPointDto> RecurrenceTrend { get; init; } = Array.Empty<TrendPointDto>();
}

// ── CAPA ──────────────────────────────────────────────────────────────────────

public sealed class CapaAnalyticsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public CapaMetricsDto Metrics { get; init; } = new();
    public double CapaEffectivenessRate { get; init; }
    public double AverageClosureDays { get; init; }
    public int PendingReviews { get; init; }
    public int RecurringIssues { get; init; }
    public IReadOnlyList<AnalyticsBreakdownItemDto> DepartmentCapaPerformance { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

// ── SLA ───────────────────────────────────────────────────────────────────────

public sealed class SlaAnalyticsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public SlaEscalationMetricsDto Metrics { get; init; } = new();
    public double SlaHealthScore { get; init; }
    public double BreachRate { get; init; }
    public double EscalationRate { get; init; }
    public double AverageResolutionHours { get; init; }
    public int CriticalDeficiencyResolutionOpen { get; init; }
    public IReadOnlyList<AnalyticsBreakdownItemDto> DepartmentSlaCompliance { get; init; } = Array.Empty<AnalyticsBreakdownItemDto>();
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

// ── Departments ───────────────────────────────────────────────────────────────

public sealed class DepartmentAnalyticsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public IReadOnlyList<DepartmentAnalyticsRowDto> Departments { get; init; } = Array.Empty<DepartmentAnalyticsRowDto>();
}

public sealed class DepartmentAnalyticsRowDto
{
    public string Department { get; init; } = string.Empty;
    public decimal ReadinessScore { get; init; }
    public int OpenDeficiencies { get; init; }
    public int CapaOpen { get; init; }
    public int SlaAtRisk { get; init; }
    public int Escalations { get; init; }
    public int TaskBacklog { get; init; }
    public int EvidencePending { get; init; }
    public int VerificationPending { get; init; }
    public AnalyticsDeepLinkDto? DeepLink { get; init; }
}

// ── Trends ────────────────────────────────────────────────────────────────────

public sealed class AnalyticsTrendsDto
{
    public AnalyticsContextDto Context { get; init; } = new();
    public IReadOnlyList<TrendSeriesDto> Series { get; init; } = Array.Empty<TrendSeriesDto>();
}

public sealed class AnalyticsTrendQueryDto
{
    public string? Metric { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public AnalyticsTrendGranularity Granularity { get; init; } = AnalyticsTrendGranularity.Daily;
}
