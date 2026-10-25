using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Analytics;

/// <summary>
/// Single KPI computation authority for cross-module analytics (Sprint 12B-0 foundation).
/// </summary>
public sealed class AnalyticsAggregationService : IAnalyticsAggregationService
{
    private readonly IDeficiencyMetricsService _deficiencyMetrics;
    private readonly IComplianceScoreService _journeyScore;
    private readonly IComplianceScoringService _frameworkScoring;
    private readonly ComplianceDbContext _db;

    public AnalyticsAggregationService(
        IDeficiencyMetricsService deficiencyMetrics,
        IComplianceScoreService journeyScore,
        IComplianceScoringService frameworkScoring,
        ComplianceDbContext db)
    {
        _deficiencyMetrics = deficiencyMetrics;
        _journeyScore = journeyScore;
        _frameworkScoring = frameworkScoring;
        _db = db;
    }

    public Task<DeficiencyMetricCountsDto> GetDeficiencyCountsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        _deficiencyMetrics.GetCountsAsync(scope, ct);

    public Task<CapaMetricsDto> GetCapaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);

    public Task<SlaEscalationMetricsDto> GetSlaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);

    public async Task<AnalyticsReadinessDto> GetReadinessAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
            _db, scope.HospitalId, scope.FrameworkId, ct);

        decimal journeyReadiness = 0;
        if (journey != null)
        {
            journeyReadiness = await _journeyScore.GetJourneyScoreAsync(journey.FrameworkId, journey.Id, ct);
        }

        var frameworkScore = await _frameworkScoring.CalculateFrameworkScoreAsync(scope.HospitalId, scope.FrameworkId);

        return new AnalyticsReadinessDto
        {
            JourneyAccreditationReadiness = journeyReadiness,
            FrameworkAssessmentReadiness = frameworkScore.OverallScore,
        };
    }

    public async Task<AnalyticsGovernanceDto> GetGovernanceMetricsAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var capa = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var sla = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);
        var counts = await _deficiencyMetrics.GetCountsAsync(scope, ct);

        var openWithCapaCompleted = await _db.ComplianceDeficiencies.AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(
                d => d.HospitalId == scope.HospitalId
                     && d.FrameworkId == scope.FrameworkId
                     && d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled
                     && d.CorrectiveActionStatus == CapaActionStatus.Completed
                     && d.PreventiveActionStatus == CapaActionStatus.Completed,
                ct);

        var capaCompletionRate = counts.Open > 0
            ? (double)openWithCapaCompleted / counts.Open * 100.0
            : 0;

        var slaHealth = sla.TotalOpenWithSla > 0
            ? (double)(sla.OnTrackCount + sla.WarningCount) / sla.TotalOpenWithSla * 100.0
            : 100.0;

        var departmentsAtRisk = await _db.ComplianceDeficiencies.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => d.HospitalId == scope.HospitalId
                        && d.FrameworkId == scope.FrameworkId
                        && d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled
                        && (d.CurrentSlaStatus == SlaStatus.AtRisk || d.CurrentSlaStatus == SlaStatus.Breached))
            .Select(d => string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        return new AnalyticsGovernanceDto
        {
            Capa = new CapaGovernanceSummaryDto
            {
                CapaEffectivenessRate = Math.Round(capa.CapaEffectivenessRate * 100.0, 1),
                RecurringIssues = capa.RecurringIssues,
                PendingReviews = capa.PendingReviews,
                CapaCompletionRate = Math.Round(capaCompletionRate, 1),
            },
            Sla = new SlaGovernanceSummaryDto
            {
                OpenCriticalEscalations = sla.CriticalEscalations,
                OverdueDeficiencies = sla.BreachedCount,
                SlaHealthScore = Math.Round(slaHealth, 1),
                DepartmentsAtRisk = departmentsAtRisk,
            },
        };
    }
}
