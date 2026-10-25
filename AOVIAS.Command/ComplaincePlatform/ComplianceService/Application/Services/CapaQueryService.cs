using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 11 — CAPA read model, lifecycle, and analytics.</summary>
public sealed class CapaQueryService : ICapaQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly IRecurrenceEvaluationService _recurrence;

    public CapaQueryService(ComplianceDbContext db, IRecurrenceEvaluationService recurrence)
    {
        _db = db;
        _recurrence = recurrence;
    }

    public async Task<DeficiencyCapaDto?> GetCapaAsync(Guid deficiencyId, CancellationToken ct = default)
    {
        var d = await _db.ComplianceDeficiencies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == deficiencyId, ct);

        if (d == null)
        {
            return null;
        }

        var correctiveStatus = ResolveActionStatus(
            d.CorrectiveActionStatus, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate);
        var preventiveStatus = ResolveActionStatus(
            d.PreventiveActionStatus, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate);

        var recurrence = await _recurrence.EvaluateAsync(deficiencyId, lookbackDays: 90, persist: true, ct);

        return new DeficiencyCapaDto
        {
            DeficiencyId = d.Id,
            DeficiencyCode = d.DeficiencyCode,
            CapaNumber = d.CapaNumber,
            Status = d.Status.ToString(),
            RootCauseCategory = d.RootCauseCategory?.ToString(),
            ProblemStatement = d.ProblemStatement,
            RootCauseSummary = d.RootCauseSummary,
            FinalRootCause = d.FinalRootCause,
            CorrectiveActionSummary = d.CorrectiveActionSummary,
            CorrectiveActionOwner = d.CorrectiveActionOwner ?? d.AssignedTo,
            CorrectiveActionStartDate = d.CorrectiveActionStartDate,
            CorrectiveActionTargetDate = d.CorrectiveActionTargetDate,
            CorrectiveActionCompletedDate = d.CorrectiveActionCompletedDate,
            CorrectiveActionStatus = FormatStatus(correctiveStatus),
            CorrectiveActionOverdue = IsOverdue(correctiveStatus, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate),
            PreventiveActionSummary = d.PreventiveActionSummary,
            PreventiveActionOwner = d.PreventiveActionOwner,
            PreventiveActionTargetDate = d.PreventiveActionTargetDate,
            PreventiveActionCompletedDate = d.PreventiveActionCompletedDate,
            PreventiveActionStatus = FormatStatus(preventiveStatus),
            PreventiveActionSuccessCriteria = d.PreventiveActionSuccessCriteria ?? d.SuccessCriteria,
            PreventiveActionOverdue = IsOverdue(preventiveStatus, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate),
            EffectivenessReviewStatus = d.EffectivenessReviewStatus.ToString(),
            EffectivenessReviewScheduledDate = d.EffectivenessReviewScheduledDate,
            EffectivenessReviewDaysOffset = d.EffectivenessReviewDaysOffset,
            EffectivenessReviewDate = d.EffectivenessReviewDate,
            EffectivenessReviewer = d.EffectivenessReviewer,
            EffectivenessReviewNotes = d.EffectivenessReviewNotes,
            EffectivenessReviewOutcome = d.EffectivenessReviewOutcome,
            IsRecurringIssue = d.IsRecurringIssue || recurrence.IsRecurring,
            RecurrenceReason = recurrence.Reason,
            RecurrenceDetectedAt = d.RecurrenceDetectedAt,
            RecurrenceAnalysis = recurrence,
            LifecycleSteps = BuildLifecycleSteps(d, correctiveStatus, preventiveStatus),
        };
    }

    public async Task<CapaMetricsDto> GetMetricsAsync(
        Guid organizationId,
        Guid? hospitalId = null,
        Guid? frameworkId = null,
        CancellationToken ct = default)
    {
        var query = _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.OrganizationId == organizationId
                        && d.Status != DeficiencyStatus.Closed
                        && d.Status != DeficiencyStatus.Cancelled);

        if (hospitalId.HasValue)
        {
            query = query.Where(d => d.HospitalId == hospitalId);
        }

        if (frameworkId.HasValue && frameworkId.Value != Guid.Empty)
        {
            query = query.Where(d => d.FrameworkId == frameworkId);
        }

        var open = await query.ToListAsync(ct);
        var closedQuery = _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.OrganizationId == organizationId
                        && d.Status == DeficiencyStatus.Closed
                        && d.ClosedDate != null);

        if (hospitalId.HasValue)
        {
            closedQuery = closedQuery.Where(d => d.HospitalId == hospitalId);
        }

        if (frameworkId.HasValue && frameworkId.Value != Guid.Empty)
        {
            closedQuery = closedQuery.Where(d => d.FrameworkId == frameworkId);
        }

        var closed = await closedQuery.ToListAsync(ct);

        var openCapas = open.Count(d =>
            d.CorrectiveActionStatus != CapaActionStatus.NotStarted
            || d.PreventiveActionStatus != CapaActionStatus.NotStarted
            || !string.IsNullOrWhiteSpace(d.CorrectiveActionSummary));

        var overdue = open.Count(d =>
            IsOverdue(ResolveActionStatus(d.CorrectiveActionStatus, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate),
                d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate)
            || IsOverdue(ResolveActionStatus(d.PreventiveActionStatus, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate),
                d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate));

        var reviewed = closed.Where(d => d.EffectivenessReviewStatus is EffectivenessReviewStatus.Effective or EffectivenessReviewStatus.NotEffective).ToList();
        var effective = reviewed.Count(d => d.EffectivenessReviewStatus == EffectivenessReviewStatus.Effective);

        var now = DateTime.UtcNow;
        var openCapaRows = open.Where(d =>
            d.CorrectiveActionStatus != CapaActionStatus.NotStarted
            || d.PreventiveActionStatus != CapaActionStatus.NotStarted
            || !string.IsNullOrWhiteSpace(d.CorrectiveActionSummary)
            || !string.IsNullOrWhiteSpace(d.CapaNumber)).ToList();

        var aging = new CapaAgingBucketsDto();
        foreach (var d in openCapaRows)
        {
            var ageDays = (now - d.DetectedDate).TotalDays;
            if (ageDays <= 30) aging.Within30++;
            else if (ageDays <= 60) aging.Days31To60++;
            else if (ageDays <= 90) aging.Days61To90++;
            else aging.Over90++;
        }

        var overdueByDept = open
            .Where(d =>
                IsOverdue(ResolveActionStatus(d.CorrectiveActionStatus, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate),
                    d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate)
                || IsOverdue(ResolveActionStatus(d.PreventiveActionStatus, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate),
                    d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate))
            .GroupBy(d => string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!)
            .Select(g => new DeficiencyCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        return new CapaMetricsDto
        {
            OpenCapas = openCapas,
            OverdueCapas = overdue,
            EffectiveCapas = effective,
            IneffectiveCapas = reviewed.Count - effective,
            RecurringIssues = open.Count(d => d.IsRecurringIssue),
            PendingReviews = open.Count(d => d.EffectivenessReviewStatus is EffectivenessReviewStatus.Pending or EffectivenessReviewStatus.Scheduled),
            CapaEffectivenessRate = reviewed.Count > 0 ? (double)effective / reviewed.Count : 0,
            RecurrenceRate = open.Count > 0 ? (double)open.Count(d => d.IsRecurringIssue) / open.Count : 0,
            AverageClosureDays = closed.Count > 0
                ? closed.Average(d => (d.ClosedDate!.Value - d.DetectedDate).TotalDays)
                : 0,
            AverageOpenCapaAgeDays = openCapaRows.Count > 0
                ? openCapaRows.Average(d => (now - d.DetectedDate).TotalDays)
                : 0,
            AgingBuckets = aging,
            TopRootCauses = open
                .GroupBy(d => d.RootCauseCategory?.ToString() ?? "Unclassified")
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new DeficiencyCountByLabelDto { Label = g.Key, Count = g.Count() })
                .ToList(),
            DepartmentCapaPerformance = open
                .GroupBy(d => string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!)
                .Select(g => new DeficiencyCountByLabelDto
                {
                    Label = g.Key,
                    Count = g.Count(x => x.CorrectiveActionStatus == CapaActionStatus.Completed),
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList(),
            DepartmentOverdueCapas = overdueByDept,
        };
    }

    public CapaActionStatus ResolveActionStatus(
        CapaActionStatus stored,
        DateTime? targetDate,
        DateTime? completedDate)
    {
        if (completedDate.HasValue || stored == CapaActionStatus.Completed)
        {
            return CapaActionStatus.Completed;
        }

        if (targetDate.HasValue && targetDate.Value < DateTime.UtcNow && stored != CapaActionStatus.Completed)
        {
            return stored == CapaActionStatus.NotStarted ? CapaActionStatus.NotStarted : CapaActionStatus.InProgress;
        }

        return stored;
    }

    public static bool IsOverdue(CapaActionStatus status, DateTime? targetDate, DateTime? completedDate) =>
        status != CapaActionStatus.Completed
        && targetDate.HasValue
        && targetDate.Value < DateTime.UtcNow
        && !completedDate.HasValue;

    private static string FormatStatus(CapaActionStatus status) => status.ToString();

    private static IReadOnlyList<CapaLifecycleStepDto> BuildLifecycleSteps(
        ComplianceDeficiency d,
        CapaActionStatus correctiveStatus,
        CapaActionStatus preventiveStatus)
    {
        var steps = new List<(string Key, string Label, bool Done)>
        {
            ("Identified", "Deficiency Identified", true),
            ("Rca", "Root Cause Analysis", !string.IsNullOrWhiteSpace(d.RootCauseSummary) || d.RootCauseCategory.HasValue),
            ("Corrective", "Corrective Action", correctiveStatus == CapaActionStatus.Completed),
            ("Preventive", "Preventive Action", preventiveStatus == CapaActionStatus.Completed),
            ("Evidence", "Evidence", d.Status is DeficiencyStatus.EvidenceSubmitted or DeficiencyStatus.ReviewPending or DeficiencyStatus.Verified or DeficiencyStatus.Closed),
            ("Verification", "Verification", d.Status is DeficiencyStatus.Verified or DeficiencyStatus.Closed),
            ("Effectiveness", "Effectiveness Review", d.EffectivenessReviewStatus is EffectivenessReviewStatus.Effective or EffectivenessReviewStatus.NotEffective),
            ("Closed", "Closed", d.Status == DeficiencyStatus.Closed),
        };

        var currentIdx = steps.FindIndex(s => !s.Done);
        if (currentIdx < 0)
        {
            currentIdx = steps.Count - 1;
        }

        return steps.Select((s, i) => new CapaLifecycleStepDto
        {
            Key = s.Key,
            Label = s.Label,
            IsComplete = s.Done,
            IsCurrent = i == currentIdx,
        }).ToList();
    }
}
