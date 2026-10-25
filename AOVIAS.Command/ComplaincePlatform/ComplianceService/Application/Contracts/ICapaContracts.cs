using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface IRecurrenceEvaluationService
{
    Task<RecurrenceEvaluationResultDto> EvaluateAsync(
        Guid deficiencyId,
        int lookbackDays = 90,
        bool persist = true,
        CancellationToken ct = default);
}

public interface ICapaQueryService
{
    Task<DeficiencyCapaDto?> GetCapaAsync(Guid deficiencyId, CancellationToken ct = default);
    Task<CapaMetricsDto> GetMetricsAsync(
        Guid organizationId,
        Guid? hospitalId = null,
        Guid? frameworkId = null,
        CancellationToken ct = default);
    CapaActionStatus ResolveActionStatus(CapaActionStatus stored, DateTime? targetDate, DateTime? completedDate);
}

public class RecurrenceEvaluationResultDto
{
    public Guid DeficiencyId { get; set; }
    public bool IsRecurring { get; set; }
    public string? Reason { get; set; }
    public int MatchCount { get; set; }
    public int LookbackDays { get; set; }
}

public class CapaAgingBucketsDto
{
    public int Within30 { get; set; }
    public int Days31To60 { get; set; }
    public int Days61To90 { get; set; }
    public int Over90 { get; set; }
}

public class DeficiencyCapaDto
{
    public Guid DeficiencyId { get; set; }
    public string DeficiencyCode { get; set; } = string.Empty;
    public string? CapaNumber { get; set; }
    public string Status { get; set; } = string.Empty;

    public string? RootCauseCategory { get; set; }
    public string? ProblemStatement { get; set; }
    public string? RootCauseSummary { get; set; }
    public string? FinalRootCause { get; set; }

    public string? CorrectiveActionSummary { get; set; }
    public string? CorrectiveActionOwner { get; set; }
    public DateTime? CorrectiveActionStartDate { get; set; }
    public DateTime? CorrectiveActionTargetDate { get; set; }
    public DateTime? CorrectiveActionCompletedDate { get; set; }
    public string CorrectiveActionStatus { get; set; } = string.Empty;
    public bool CorrectiveActionOverdue { get; set; }

    public string? PreventiveActionSummary { get; set; }
    public string? PreventiveActionOwner { get; set; }
    public DateTime? PreventiveActionTargetDate { get; set; }
    public DateTime? PreventiveActionCompletedDate { get; set; }
    public string PreventiveActionStatus { get; set; } = string.Empty;
    public string? PreventiveActionSuccessCriteria { get; set; }
    public bool PreventiveActionOverdue { get; set; }

    public string EffectivenessReviewStatus { get; set; } = string.Empty;
    public DateTime? EffectivenessReviewScheduledDate { get; set; }
    public int? EffectivenessReviewDaysOffset { get; set; }
    public DateTime? EffectivenessReviewDate { get; set; }
    public string? EffectivenessReviewer { get; set; }
    public string? EffectivenessReviewNotes { get; set; }
    public string? EffectivenessReviewOutcome { get; set; }

    public bool IsRecurringIssue { get; set; }
    public string? RecurrenceReason { get; set; }
    public DateTime? RecurrenceDetectedAt { get; set; }
    public RecurrenceEvaluationResultDto? RecurrenceAnalysis { get; set; }

    public IReadOnlyList<CapaLifecycleStepDto> LifecycleSteps { get; set; } = Array.Empty<CapaLifecycleStepDto>();
}

public class CapaMetricsDto
{
    public int OpenCapas { get; set; }
    public int OverdueCapas { get; set; }
    public int EffectiveCapas { get; set; }
    public int IneffectiveCapas { get; set; }
    public int RecurringIssues { get; set; }
    public int PendingReviews { get; set; }
    public double CapaEffectivenessRate { get; set; }
    public double RecurrenceRate { get; set; }
    public double AverageClosureDays { get; set; }
    public double AverageOpenCapaAgeDays { get; set; }
    public CapaAgingBucketsDto AgingBuckets { get; set; } = new();
    public IReadOnlyList<DeficiencyCountByLabelDto> TopRootCauses { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
    public IReadOnlyList<DeficiencyCountByLabelDto> DepartmentCapaPerformance { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
    public IReadOnlyList<DeficiencyCountByLabelDto> DepartmentOverdueCapas { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
}
