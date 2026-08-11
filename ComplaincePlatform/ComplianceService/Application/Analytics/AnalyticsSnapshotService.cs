using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Maturity;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ComplianceService.Application.Services;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;
using AppIComplianceScoringService = ComplianceService.Application.Contracts.IComplianceScoringService;

namespace ComplianceService.Application.Analytics;

public sealed class AnalyticsSnapshotService : IAnalyticsSnapshotService
{
    private readonly ComplianceDbContext _db;
    private readonly IAnalyticsAggregationService _aggregation;
    private readonly IDeficiencyMetricsService _deficiencyMetrics;
    private readonly ITaskQueryService _taskQuery;
    private readonly IEvidenceLibraryService _evidenceLibrary;
    private readonly ComplianceMaturityIndexService _cmiService;
    private readonly AppIComplianceScoringService _frameworkScoring;
    private readonly IComplianceScoreService _journeyScore;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly ILogger<AnalyticsSnapshotService> _logger;

    public AnalyticsSnapshotService(
        ComplianceDbContext db,
        IAnalyticsAggregationService aggregation,
        IDeficiencyMetricsService deficiencyMetrics,
        ITaskQueryService taskQuery,
        IEvidenceLibraryService evidenceLibrary,
        ComplianceMaturityIndexService cmiService,
        AppIComplianceScoringService frameworkScoring,
        IComplianceScoreService journeyScore,
        IStageUnlockingService stageUnlocking,
        ILogger<AnalyticsSnapshotService> logger)
    {
        _db = db;
        _aggregation = aggregation;
        _deficiencyMetrics = deficiencyMetrics;
        _taskQuery = taskQuery;
        _evidenceLibrary = evidenceLibrary;
        _cmiService = cmiService;
        _frameworkScoring = frameworkScoring;
        _journeyScore = journeyScore;
        _stageUnlocking = stageUnlocking;
        _logger = logger;
    }

    public async Task GenerateDailySnapshotAsync(AnalyticsScope scope, DateOnly? date = null, CancellationToken ct = default)
    {
        if (scope.FrameworkId == Guid.Empty)
        {
            _logger.LogWarning("Skipping analytics daily snapshot — framework not resolved for org {OrgId}", scope.OrganizationId);
            return;
        }

        var snapshotDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var exists = await _db.AnalyticsDailySnapshots.AnyAsync(
            s => s.OrganizationId == scope.OrganizationId
                 && s.HospitalId == scope.HospitalId
                 && s.FrameworkId == scope.FrameworkId
                 && s.SnapshotDate == snapshotDate,
            ct);

        if (exists)
        {
            _logger.LogDebug(
                "Analytics daily snapshot already exists for {Date} org={OrgId} hospital={HospitalId}",
                snapshotDate, scope.OrganizationId, scope.HospitalId);
            return;
        }

        var readiness = await _aggregation.GetReadinessAsync(scope, ct);
        var counts = await _aggregation.GetDeficiencyCountsAsync(scope, ct);
        var capaMetrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);
        var taskSummary = await _taskQuery.GetSummaryAsync(
            new Application.DTOs.TaskQuerySpec { FrameworkId = scope.FrameworkId, HospitalId = scope.HospitalId },
            ct);
        var evidenceSummary = await _evidenceLibrary.GetSummaryAsync(scope.HospitalId, scope.FrameworkId, ct);
        var cmi = await ComputeCmiAsync(scope, ct);
        var stageUnlockedCount = await CountUnlockedStagesAsync(scope, ct);

        var snapshot = new AnalyticsDailySnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotDate = snapshotDate,
            OrganizationId = scope.OrganizationId,
            HospitalId = scope.HospitalId,
            FrameworkId = scope.FrameworkId,
            ReadinessScore = readiness.JourneyAccreditationReadiness,
            ComplianceMaturityIndex = cmi.Value,
            OpenDeficiencies = counts.Open,
            CriticalDeficiencies = counts.CriticalOpen,
            CapaOpen = capaMetrics.OpenCapas,
            CapaEffective = capaMetrics.EffectiveCapas,
            CapaIneffective = capaMetrics.IneffectiveCapas,
            EscalationsOpen = slaMetrics.EscalatedCount,
            SlaBreached = slaMetrics.BreachedCount,
            StageUnlockedCount = stageUnlockedCount,
            RecurringIssues = capaMetrics.RecurringIssues,
            EvidencePendingVerification = evidenceSummary.PendingReview,
            TaskBacklog = taskSummary.NotStarted + taskSummary.InProgress + taskSummary.Blocked,
            CreatedAt = DateTime.UtcNow,
        };

        _db.AnalyticsDailySnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Persisted analytics daily snapshot for {Date} org={OrgId} hospital={HospitalId}",
            snapshotDate, scope.OrganizationId, scope.HospitalId);
    }

    public async Task GenerateAllOrganizationsDailyAsync(DateOnly? date = null, CancellationToken ct = default)
    {
        var snapshotDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var scopes = await _db.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(j => new { j.OrganizationId, j.HospitalId, j.FrameworkId })
            .Distinct()
            .ToListAsync(ct);

        foreach (var scopeRow in scopes)
        {
            try
            {
                var scope = AnalyticsScope.ForHospital(scopeRow.OrganizationId, scopeRow.HospitalId, scopeRow.FrameworkId);
                await GenerateDailySnapshotAsync(scope, snapshotDate, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed analytics daily snapshot for org={OrgId} hospital={HospitalId}",
                    scopeRow.OrganizationId,
                    scopeRow.HospitalId);
            }
        }
    }

    public async Task<IReadOnlyList<AnalyticsDailySnapshot>> GetDailySnapshotsAsync(
        AnalyticsScope scope,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var rows = await _db.AnalyticsDailySnapshots
            .AsNoTracking()
            .Where(s => s.OrganizationId == scope.OrganizationId
                        && s.HospitalId == scope.HospitalId
                        && s.FrameworkId == scope.FrameworkId
                        && s.SnapshotDate >= from
                        && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);
        return rows;
    }

    private async Task<Application.DTOs.ComplianceMaturityIndexDto> ComputeCmiAsync(AnalyticsScope scope, CancellationToken ct)
    {
        decimal coreScore = 0;
        decimal commitmentScore = 0;
        decimal achievementScore = 0;
        decimal excellenceScore = 0;

        try
        {
            var scoreResult = await _frameworkScoring.CalculateFrameworkScoreAsync(scope.HospitalId, scope.FrameworkId);
            coreScore = scoreResult.OverallScore;

            var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
                _db, scope.HospitalId, scope.FrameworkId, ct);

            if (journey != null)
            {
                commitmentScore = await _journeyScore.GetStageScoreAsync(journey.Id, OeClassification.Commitment, ct);
                achievementScore = await _journeyScore.GetStageScoreAsync(journey.Id, OeClassification.Achievement, ct);
                excellenceScore = await _journeyScore.GetStageScoreAsync(journey.Id, OeClassification.Excellence, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CMI calculation failed during snapshot for scope {Scope}", scope);
        }

        return _cmiService.Calculate(coreScore, commitmentScore, achievementScore, excellenceScore);
    }

    private async Task<int> CountUnlockedStagesAsync(AnalyticsScope scope, CancellationToken ct)
    {
        try
        {
            var evaluations = await _stageUnlocking.GetStageEvaluationsAsync(
                scope.HospitalId, scope.OrganizationId, scope.FrameworkId, ct);
            return evaluations.Count(e => e.CriteriaMet);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stage unlock count failed for scope {Scope}", scope);
            return 0;
        }
    }
}
