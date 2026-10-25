using System.Globalization;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Maturity;
using ComplianceService.Application.Services;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;
using AppIComplianceScoringService = ComplianceService.Application.Contracts.IComplianceScoringService;

namespace ComplianceService.Application.Analytics;

public sealed class AnalyticsQueryService : IAnalyticsQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly IAnalyticsAggregationService _aggregation;
    private readonly IDeficiencyMetricsService _deficiencyMetrics;
    private readonly AppIComplianceScoringService _frameworkScoring;
    private readonly IComplianceScoreService _journeyScore;
    private readonly ComplianceMaturityIndexService _cmiService;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly IObligationQueryService _obligationQuery;
    private readonly ITaskQueryService _taskQuery;
    private readonly IEvidenceLibraryService _evidenceLibrary;
    private readonly ILogger<AnalyticsQueryService> _logger;

    public AnalyticsQueryService(
        ComplianceDbContext db,
        IAnalyticsAggregationService aggregation,
        IDeficiencyMetricsService deficiencyMetrics,
        AppIComplianceScoringService frameworkScoring,
        IComplianceScoreService journeyScore,
        ComplianceMaturityIndexService cmiService,
        IStageUnlockingService stageUnlocking,
        IObligationQueryService obligationQuery,
        ITaskQueryService taskQuery,
        IEvidenceLibraryService evidenceLibrary,
        ILogger<AnalyticsQueryService> logger)
    {
        _db = db;
        _aggregation = aggregation;
        _deficiencyMetrics = deficiencyMetrics;
        _frameworkScoring = frameworkScoring;
        _journeyScore = journeyScore;
        _cmiService = cmiService;
        _stageUnlocking = stageUnlocking;
        _obligationQuery = obligationQuery;
        _taskQuery = taskQuery;
        _evidenceLibrary = evidenceLibrary;
        _logger = logger;
    }

    public async Task<AnalyticsScope> ResolveScopeAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId,
        CancellationToken ct = default)
    {
        if (frameworkId is { } fw && fw != Guid.Empty)
        {
            return AnalyticsScope.ForHospital(organizationId, hospitalId, fw);
        }

        var resolved = await _db.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.UpdatedAt)
            .Select(j => j.FrameworkId)
            .FirstOrDefaultAsync(ct);

        return AnalyticsScope.ForHospital(organizationId, hospitalId, resolved);
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var readiness = await _aggregation.GetReadinessAsync(scope, ct);
        var counts = await _aggregation.GetDeficiencyCountsAsync(scope, ct);
        var governance = await _aggregation.GetGovernanceMetricsAsync(scope, ct);
        var cmi = await ComputeCmiAsync(scope, ct);
        var currentStage = await ResolveCurrentStageLabelAsync(scope, cmi.CoreScore, cmi.CommitmentScore, cmi.AchievementScore, ct);

        var capaMetrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);

        return new AnalyticsOverviewDto
        {
            Context = context,
            ReadinessScore = readiness.JourneyAccreditationReadiness,
            ComplianceMaturityIndex = cmi.Value,
            OpenDeficiencies = counts.Open,
            CriticalDeficiencies = counts.CriticalOpen,
            CapaEffectivenessRate = Math.Round(capaMetrics.CapaEffectivenessRate * 100.0, 1),
            SlaHealthScore = governance.Sla.SlaHealthScore,
            EscalationRate = Math.Round(slaMetrics.EscalationRate * 100.0, 1),
            RecurringIssues = capaMetrics.RecurringIssues,
            PendingReviews = capaMetrics.PendingReviews,
            CurrentStage = currentStage,
            KpiCards = BuildOverviewKpiCards(readiness, counts, governance, capaMetrics, slaMetrics, cmi.Value, currentStage),
        };
    }

    public async Task<ExecutiveAnalyticsDto> GetExecutiveAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var readiness = await _aggregation.GetReadinessAsync(scope, ct);
        var governance = await _aggregation.GetGovernanceMetricsAsync(scope, ct);
        var counts = await _aggregation.GetDeficiencyCountsAsync(scope, ct);
        var capaMetrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);
        var openRows = await _deficiencyMetrics.GetScopedOpenAsync(scope, ct);

        var deptPerformance = capaMetrics.DepartmentCapaPerformance
            .Select(d => new AnalyticsBreakdownItemDto
            {
                Label = d.Label,
                Count = d.Count,
                DeepLink = AnalyticsDeepLinks.DepartmentDeficiencies(d.Label),
            })
            .ToList();

        var highRisk = openRows.Count(d => d.AssessedRiskLevel == DeficiencyRiskLevel.High);

        return new ExecutiveAnalyticsDto
        {
            Context = context,
            ComplianceReadiness = new AnalyticsReadinessSectionDto
            {
                Score = readiness.FrameworkAssessmentReadiness,
                Source = readiness.FrameworkReadinessSource,
                DeepLink = AnalyticsDeepLinks.ComplianceMaturity(),
            },
            AccreditationReadiness = new AnalyticsReadinessSectionDto
            {
                Score = readiness.JourneyAccreditationReadiness,
                Source = readiness.JourneyReadinessSource,
                DeepLink = AnalyticsDeepLinks.Journey(),
            },
            DepartmentPerformance = deptPerformance,
            CapaPerformance = governance.Capa,
            SlaHealth = governance.Sla,
            RiskExposure = new AnalyticsRiskExposureDto
            {
                CriticalOpenDeficiencies = counts.CriticalOpen,
                HighRiskDeficiencies = highRisk,
                CriticalRiskCount = openRows.Count(d => DeficiencyMetricDefinitions.IsCriticalRisk(d)),
                DeepLink = AnalyticsDeepLinks.CriticalDeficiencies(),
            },
            EscalationExposure = new AnalyticsEscalationExposureDto
            {
                OpenEscalations = slaMetrics.EscalatedCount,
                CriticalEscalations = slaMetrics.CriticalEscalations,
                EscalationRate = Math.Round(slaMetrics.EscalationRate * 100.0, 1),
                DeepLink = AnalyticsDeepLinks.Escalations(),
            },
        };
    }

    public async Task<OperationalAnalyticsDto> GetOperationalAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var counts = await _aggregation.GetDeficiencyCountsAsync(scope, ct);
        var capaMetrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);
        var governance = await _aggregation.GetGovernanceMetricsAsync(scope, ct);
        var rows = await LoadScopedDeficienciesAsync(scope, ct);
        var openRows = rows.Where(DeficiencyMetricDefinitions.IsOpen).ToList();

        var funnel = new AnalyticsDeficiencyFunnelDto
        {
            Open = counts.Open,
            AwaitingAssignment = openRows.Count(d => d.Status == DeficiencyStatus.Identified),
            InProgress = openRows.Count(d => d.Status == DeficiencyStatus.InProgress),
            AwaitingEvidence = openRows.Count(d => d.Status == DeficiencyStatus.EvidenceSubmitted),
            VerificationPending = openRows.Count(d => d.Status == DeficiencyStatus.ReviewPending),
            Verified = rows.Count(d => d.Status == DeficiencyStatus.Verified),
            Closed = counts.Closed,
            DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter(),
        };

        var taskSummary = await _taskQuery.GetSummaryAsync(new TaskQuerySpec { FrameworkId = scope.FrameworkId }, ct);
        var evidenceSummary = await _evidenceLibrary.GetSummaryAsync(scope.HospitalId, scope.FrameworkId, ct);

        var rootCauseTrend = openRows
            .GroupBy(d => d.RootCauseCategory?.ToString() ?? "Unclassified")
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new AnalyticsBreakdownItemDto
            {
                Label = g.Key,
                Count = g.Count(),
                DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter($"Root Cause: {g.Key}", "open"),
            })
            .ToList();

        var auditQuery = _db.AuditSessions.AsNoTracking()
            .Where(s => s.TenantId == scope.OrganizationId);
        if (scope.HospitalId != Guid.Empty)
        {
            auditQuery = auditQuery.Where(s => s.HospitalId == null || s.HospitalId == scope.HospitalId);
        }

        if (scope.FrameworkId != Guid.Empty)
        {
            auditQuery = auditQuery.Where(s => s.FrameworkId == null || s.FrameworkId == scope.FrameworkId);
        }

        var auditTotal = await auditQuery.CountAsync(ct);
        var auditClosedOrSubmitted = await auditQuery.CountAsync(
            s => s.Status == AuditSessionStatus.Closed || s.Status == AuditSessionStatus.Submitted, ct);
        var auditCompletionRate = auditTotal > 0
            ? Math.Round(100.0 * auditClosedOrSubmitted / auditTotal, 1)
            : 0;

        var overdueDeficiencies = openRows.Count(d =>
            d.DueDate.HasValue && d.DueDate.Value < DateTime.UtcNow);
        var slaComplianceRate = Math.Max(0, Math.Round(100.0 - (slaMetrics.BreachRate * 100.0), 1));

        return new OperationalAnalyticsDto
        {
            Context = context,
            DeficiencyFunnel = funnel,
            TaskThroughput = taskSummary,
            EvidencePipeline = evidenceSummary,
            VerificationBacklog = evidenceSummary.PendingReview,
            AssignmentLoad = openRows.Count(d => string.IsNullOrWhiteSpace(d.AssignedTo)),
            RecurringIssues = capaMetrics.RecurringIssues,
            RootCauseTrend = rootCauseTrend,
            CapaAging = capaMetrics.AgingBuckets ?? new CapaAgingBucketsDto(),
            AverageOpenCapaAgeDays = Math.Round(capaMetrics.AverageOpenCapaAgeDays, 1),
            AverageResolutionHours = Math.Round(slaMetrics.AverageResolutionHours, 1),
            AverageResolutionDays = Math.Round(capaMetrics.AverageClosureDays, 1),
            AuditCompletionRate = auditCompletionRate,
            AuditSessionsTotal = auditTotal,
            AuditSessionsClosedOrSubmitted = auditClosedOrSubmitted,
            DepartmentPerformance = capaMetrics.DepartmentCapaPerformance
                .Select(d => new AnalyticsBreakdownItemDto
                {
                    Label = d.Label,
                    Count = d.Count,
                    DeepLink = AnalyticsDeepLinks.DepartmentDeficiencies(d.Label),
                })
                .ToList(),
            SlaComplianceRate = slaComplianceRate,
            SlaHealthScore = governance.Sla.SlaHealthScore,
            OverdueWork = new OperationalOverdueWorkDto
            {
                OverdueCapas = capaMetrics.OverdueCapas,
                OverdueTasks = taskSummary.Overdue,
                OverdueDeficiencies = overdueDeficiencies,
                DepartmentOverdueCapas = (capaMetrics.DepartmentOverdueCapas ?? Array.Empty<DeficiencyCountByLabelDto>())
                    .Select(d => new AnalyticsBreakdownItemDto
                    {
                        Label = d.Label,
                        Count = d.Count,
                        DeepLink = AnalyticsDeepLinks.DepartmentDeficiencies(d.Label),
                    })
                    .ToList(),
            },
            DeepLinks =
            [
                AnalyticsDeepLinks.DeficiencyCommandCenter(),
                AnalyticsDeepLinks.Tasks(),
                AnalyticsDeepLinks.EvidenceReview(),
                AnalyticsDeepLinks.Findings(),
            ],
        };
    }

    public async Task<RcaAnalyticsDto> GetRcaAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var enriched = await LoadDeficienciesWithChapterAsync(scope, ct);
        var openRows = enriched.Where(x => DeficiencyMetricDefinitions.IsOpen(x.Deficiency)).ToList();

        var topRootCauses = ToBreakdown(openRows, x => x.Deficiency.RootCauseCategory?.ToString() ?? "Unclassified");
        var byDepartment = ToBreakdown(openRows, x => string.IsNullOrWhiteSpace(x.Deficiency.Department) ? "Unassigned" : x.Deficiency.Department!);
        var byChapter = ToBreakdown(openRows, x => x.ChapterLabel);
        var byStage = ToBreakdown(openRows, x => x.Deficiency.MaturityLevel.ToString());

        var rootCauseTrend = await BuildDailyMetricTrendAsync(scope, "recurringIssues", ct);
        var recurrenceTrend = await BuildDailyMetricTrendAsync(scope, "openDeficiencies", ct);

        return new RcaAnalyticsDto
        {
            Context = context,
            TopRootCauses = topRootCauses,
            RootCauseByDepartment = byDepartment,
            RootCauseByChapter = byChapter,
            RootCauseByStage = byStage,
            RootCauseTrend = rootCauseTrend,
            RecurrenceTrend = recurrenceTrend,
        };
    }

    public async Task<CapaAnalyticsDto> GetCapaAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var metrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);

        return new CapaAnalyticsDto
        {
            Context = context,
            Metrics = metrics,
            CapaEffectivenessRate = Math.Round(metrics.CapaEffectivenessRate * 100.0, 1),
            AverageClosureDays = metrics.AverageClosureDays,
            PendingReviews = metrics.PendingReviews,
            RecurringIssues = metrics.RecurringIssues,
            DepartmentCapaPerformance = metrics.DepartmentCapaPerformance
                .Select(d => new AnalyticsBreakdownItemDto
                {
                    Label = d.Label,
                    Count = d.Count,
                    DeepLink = AnalyticsDeepLinks.DepartmentDeficiencies(d.Label),
                })
                .ToList(),
            DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter("Open CAPA Workbench", "capa"),
        };
    }

    public async Task<SlaAnalyticsDto> GetSlaAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var metrics = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);
        var counts = await _aggregation.GetDeficiencyCountsAsync(scope, ct);
        var governance = await _aggregation.GetGovernanceMetricsAsync(scope, ct);

        return new SlaAnalyticsDto
        {
            Context = context,
            Metrics = metrics,
            SlaHealthScore = governance.Sla.SlaHealthScore,
            BreachRate = Math.Round(metrics.BreachRate * 100.0, 1),
            EscalationRate = Math.Round(metrics.EscalationRate * 100.0, 1),
            AverageResolutionHours = metrics.AverageResolutionHours,
            CriticalDeficiencyResolutionOpen = counts.CriticalOpen,
            DepartmentSlaCompliance = metrics.DepartmentSlaCompliance
                .Select(d => new AnalyticsBreakdownItemDto
                {
                    Label = d.Label,
                    Count = d.Count,
                    DeepLink = AnalyticsDeepLinks.DepartmentDeficiencies(d.Label),
                })
                .ToList(),
            DeepLink = AnalyticsDeepLinks.SlaBreaches(),
        };
    }

    public async Task<DepartmentAnalyticsDto> GetDepartmentsAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var capaMetrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct);
        var rows = await LoadScopedDeficienciesAsync(scope, ct);
        var openRows = rows.Where(DeficiencyMetricDefinitions.IsOpen).ToList();
        var obligationSummary = await _obligationQuery.GetSummaryAsync(scope.OrganizationId, scope.FrameworkId, ct);

        var departments = openRows
            .Select(d => string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Union(capaMetrics.DepartmentCapaPerformance.Select(d => d.Label))
            .Union(slaMetrics.DepartmentSlaCompliance.Select(d => d.Label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d)
            .ToList();

        var capaByDept = capaMetrics.DepartmentCapaPerformance.ToDictionary(d => d.Label, d => d.Count, StringComparer.OrdinalIgnoreCase);
        var slaByDept = slaMetrics.DepartmentSlaCompliance.ToDictionary(d => d.Label, d => d.Count, StringComparer.OrdinalIgnoreCase);

        var result = departments.Select(dept =>
        {
            var deptOpen = openRows.Where(d =>
                string.Equals(string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department, dept, StringComparison.OrdinalIgnoreCase)).ToList();

            return new DepartmentAnalyticsRowDto
            {
                Department = dept,
                ReadinessScore = obligationSummary.OverallReadinessScore,
                OpenDeficiencies = deptOpen.Count,
                CapaOpen = capaByDept.GetValueOrDefault(dept),
                SlaAtRisk = slaByDept.GetValueOrDefault(dept),
                Escalations = deptOpen.Count(d => DeficiencyMetricDefinitions.IsEscalated(d)),
                TaskBacklog = deptOpen.Count(d => d.Status is DeficiencyStatus.Identified or DeficiencyStatus.InProgress),
                EvidencePending = deptOpen.Count(d => d.Status == DeficiencyStatus.EvidenceSubmitted),
                VerificationPending = deptOpen.Count(d => d.Status == DeficiencyStatus.ReviewPending),
                DeepLink = AnalyticsDeepLinks.DepartmentDeficiencies(dept),
            };
        }).ToList();

        return new DepartmentAnalyticsDto { Context = context, Departments = result };
    }

    public async Task<AnalyticsTrendsDto> GetTrendsAsync(
        AnalyticsScope scope,
        AnalyticsTrendQueryDto query,
        CancellationToken ct = default)
    {
        var context = await BuildContextAsync(scope, ct);
        var from = query.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));
        var to = query.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var series = new List<TrendSeriesDto>();

        var journeyTrends = await JourneyTrendBuilder.BuildAsync(_db, scope.HospitalId, scope.FrameworkId, ct: ct);

        if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("readiness", StringComparison.OrdinalIgnoreCase))
        {
            series.Add(new TrendSeriesDto
            {
                MetricKey = "readiness",
                Label = "Accreditation Readiness",
                Granularity = query.Granularity,
                Points = journeyTrends.ReadinessTrend,
            });
        }

        if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("overallScore", StringComparison.OrdinalIgnoreCase))
        {
            series.Add(new TrendSeriesDto
            {
                MetricKey = "overallScore",
                Label = "Overall Compliance Score",
                Granularity = query.Granularity,
                Points = journeyTrends.OverallScoreTrend,
            });
        }

        if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("chapterProgress", StringComparison.OrdinalIgnoreCase))
        {
            series.Add(new TrendSeriesDto
            {
                MetricKey = "chapterProgress",
                Label = "Chapter Improvement",
                Granularity = query.Granularity,
                Points = journeyTrends.ChapterImprovementTrend,
            });
        }

        if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("risk", StringComparison.OrdinalIgnoreCase))
        {
            series.Add(new TrendSeriesDto
            {
                MetricKey = "risk",
                Label = "Critical Risk Evolution",
                Granularity = query.Granularity,
                Points = journeyTrends.RiskEvolutionTrend,
            });
        }

        var dailySnapshots = await _db.AnalyticsDailySnapshots
            .AsNoTracking()
            .Where(s => s.OrganizationId == scope.OrganizationId
                        && s.HospitalId == scope.HospitalId
                        && s.FrameworkId == scope.FrameworkId
                        && s.SnapshotDate >= from
                        && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);

        if (dailySnapshots.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("openDeficiencies", StringComparison.OrdinalIgnoreCase))
            {
                series.Add(BuildDailySeries("openDeficiencies", "Open Deficiencies", query.Granularity,
                    dailySnapshots, s => s.OpenDeficiencies));
            }

            if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("cmi", StringComparison.OrdinalIgnoreCase))
            {
                series.Add(BuildDailySeries("cmi", "Compliance Maturity Index", query.Granularity,
                    dailySnapshots, s => (decimal)s.ComplianceMaturityIndex));
            }

            if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("slaBreached", StringComparison.OrdinalIgnoreCase))
            {
                series.Add(BuildDailySeries("slaBreached", "SLA Breaches", query.Granularity,
                    dailySnapshots, s => s.SlaBreached));
            }

            if (string.IsNullOrWhiteSpace(query.Metric) || query.Metric.Equals("recurringIssues", StringComparison.OrdinalIgnoreCase))
            {
                series.Add(BuildDailySeries("recurringIssues", "Recurring Issues", query.Granularity,
                    dailySnapshots, s => s.RecurringIssues));
            }
        }

        return new AnalyticsTrendsDto { Context = context, Series = series };
    }

    private async Task<AnalyticsContextDto> BuildContextAsync(AnalyticsScope scope, CancellationToken ct)
    {
        var frameworkName = scope.FrameworkId == Guid.Empty
            ? null
            : await _db.ComplianceFrameworks.AsNoTracking()
                .Where(f => f.Id == scope.FrameworkId)
                .Select(f => f.Name + " " + f.Version)
                .FirstOrDefaultAsync(ct);

        return new AnalyticsContextDto
        {
            OrganizationId = scope.OrganizationId,
            HospitalId = scope.HospitalId,
            FrameworkId = scope.FrameworkId,
            FrameworkName = frameworkName?.Trim(),
            GeneratedAt = DateTime.UtcNow,
        };
    }

    private async Task<ComplianceMaturityIndexDto> ComputeCmiAsync(AnalyticsScope scope, CancellationToken ct)
    {
        if (scope.FrameworkId == Guid.Empty)
        {
            return _cmiService.Calculate(0, 0, 0, 0);
        }

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
            _logger.LogWarning(ex, "CMI calculation failed for scope {Scope}", scope);
        }

        return _cmiService.Calculate(coreScore, commitmentScore, achievementScore, excellenceScore);
    }

    private async Task<string> ResolveCurrentStageLabelAsync(
        AnalyticsScope scope,
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore,
        CancellationToken ct)
    {
        try
        {
            var evaluations = await _stageUnlocking.GetStageEvaluationsAsync(
                scope.HospitalId, scope.OrganizationId, scope.FrameworkId, ct);
            return MaturityStagePresentationMapper.DetermineCurrentStage(
                evaluations, coreScore, commitmentScore, achievementScore).ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stage resolution failed for scope {Scope}", scope);
            return MaturityStage.Core.ToString();
        }
    }

    private static IReadOnlyList<AnalyticsMetricCardDto> BuildOverviewKpiCards(
        AnalyticsReadinessDto readiness,
        DeficiencyMetricCountsDto counts,
        AnalyticsGovernanceDto governance,
        CapaMetricsDto capaMetrics,
        SlaEscalationMetricsDto slaMetrics,
        decimal cmi,
        string currentStage) =>
    [
        new() { Key = "readiness", Label = "Accreditation Readiness", Value = readiness.JourneyAccreditationReadiness, Unit = "%", Source = readiness.JourneyReadinessSource, DeepLink = AnalyticsDeepLinks.Journey() },
        new() { Key = "cmi", Label = "Compliance Maturity Index", Value = cmi, Unit = "CMI", DeepLink = AnalyticsDeepLinks.ComplianceMaturity() },
        new() { Key = "openDeficiencies", Label = "Open Deficiencies", Value = counts.Open, DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter() },
        new() { Key = "criticalDeficiencies", Label = "Critical Deficiencies", Value = counts.CriticalOpen, DeepLink = AnalyticsDeepLinks.CriticalDeficiencies() },
        new() { Key = "capaEffectiveness", Label = "CAPA Effectiveness", Value = (decimal)Math.Round(capaMetrics.CapaEffectivenessRate * 100.0, 1), Unit = "%", DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter("CAPA", "capa") },
        new() { Key = "slaHealth", Label = "SLA Health", Value = (decimal)governance.Sla.SlaHealthScore, Unit = "%", DeepLink = AnalyticsDeepLinks.SlaBreaches() },
        new() { Key = "escalationRate", Label = "Escalation Rate", Value = (decimal)Math.Round(slaMetrics.EscalationRate * 100.0, 1), Unit = "%", DeepLink = AnalyticsDeepLinks.Escalations() },
        new() { Key = "recurringIssues", Label = "Recurring Issues", Value = capaMetrics.RecurringIssues, DeepLink = AnalyticsDeepLinks.RecurringIssues() },
        new() { Key = "pendingReviews", Label = "Pending CAPA Reviews", Value = capaMetrics.PendingReviews, DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter("Reviews", "capa") },
        new() { Key = "currentStage", Label = "Current Stage", Value = null, Unit = currentStage, DeepLink = AnalyticsDeepLinks.ComplianceMaturity() },
    ];

    private async Task<List<ComplianceDeficiency>> LoadScopedDeficienciesAsync(AnalyticsScope scope, CancellationToken ct) =>
        await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.HospitalId == scope.HospitalId && d.FrameworkId == scope.FrameworkId)
            .ToListAsync(ct);

    private async Task<List<(ComplianceDeficiency Deficiency, string ChapterLabel)>> LoadDeficienciesWithChapterAsync(
        AnalyticsScope scope,
        CancellationToken ct)
    {
        var rows = await (
            from d in _db.ComplianceDeficiencies.IgnoreQueryFilters().AsNoTracking()
            where d.HospitalId == scope.HospitalId && d.FrameworkId == scope.FrameworkId
            join oe in _db.ObjectiveElements.AsNoTracking() on d.ObjectiveElementId equals oe.Id into oeJoin
            from oe in oeJoin.DefaultIfEmpty()
            join std in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals std.Id into stdJoin
            from std in stdJoin.DefaultIfEmpty()
            join ch in _db.ComplianceChapters.AsNoTracking() on std.ChapterId equals ch.Id into chJoin
            from ch in chJoin.DefaultIfEmpty()
            select new
            {
                Deficiency = d,
                ChapterLabel = ch != null ? (ch.Code + " — " + ch.Name).Trim() : "Unknown Chapter",
            }).ToListAsync(ct);

        return rows.Select(r => (r.Deficiency, r.ChapterLabel)).ToList();
    }

    private static IReadOnlyList<AnalyticsBreakdownItemDto> ToBreakdown<T>(
        IEnumerable<T> rows,
        Func<T, string> labelSelector) =>
        rows.GroupBy(labelSelector, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => new AnalyticsBreakdownItemDto
            {
                Label = g.Key,
                Count = g.Count(),
                DeepLink = AnalyticsDeepLinks.DeficiencyCommandCenter(g.Key, "open"),
            })
            .ToList();

    private async Task<IReadOnlyList<TrendPointDto>> BuildDailyMetricTrendAsync(
        AnalyticsScope scope,
        string metricKey,
        CancellationToken ct)
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        var snapshots = await _db.AnalyticsDailySnapshots
            .AsNoTracking()
            .Where(s => s.OrganizationId == scope.OrganizationId
                        && s.HospitalId == scope.HospitalId
                        && s.FrameworkId == scope.FrameworkId
                        && s.SnapshotDate >= from
                        && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(ct);

        if (snapshots.Count == 0)
        {
            return Array.Empty<TrendPointDto>();
        }

        Func<Domain.AnalyticsDailySnapshot, decimal> selector = metricKey switch
        {
            "recurringIssues" => s => s.RecurringIssues,
            "openDeficiencies" => s => s.OpenDeficiencies,
            _ => s => s.OpenDeficiencies,
        };

        return snapshots
            .Select(s => new TrendPointDto { Label = s.SnapshotDate.ToString("yyyy-MM-dd"), Value = selector(s) })
            .ToList();
    }

    private static TrendSeriesDto BuildDailySeries(
        string metricKey,
        string label,
        AnalyticsTrendGranularity granularity,
        IReadOnlyList<Domain.AnalyticsDailySnapshot> snapshots,
        Func<Domain.AnalyticsDailySnapshot, decimal> selector)
    {
        var points = AggregateDailyPoints(snapshots, granularity, selector);
        return new TrendSeriesDto
        {
            MetricKey = metricKey,
            Label = label,
            Granularity = granularity,
            Points = points,
        };
    }

    private static IReadOnlyList<TrendPointDto> AggregateDailyPoints(
        IReadOnlyList<Domain.AnalyticsDailySnapshot> snapshots,
        AnalyticsTrendGranularity granularity,
        Func<Domain.AnalyticsDailySnapshot, decimal> selector)
    {
        if (granularity == AnalyticsTrendGranularity.Daily)
        {
            return snapshots
                .Select(s => new TrendPointDto { Label = s.SnapshotDate.ToString("yyyy-MM-dd"), Value = selector(s) })
                .ToList();
        }

        IEnumerable<IGrouping<string, Domain.AnalyticsDailySnapshot>> groups = granularity switch
        {
            AnalyticsTrendGranularity.Weekly => snapshots.GroupBy(s =>
                $"{s.SnapshotDate.Year}-W{ISOWeek.GetWeekOfYear(s.SnapshotDate.ToDateTime(TimeOnly.MinValue)):D2}"),
            AnalyticsTrendGranularity.Monthly => snapshots.GroupBy(s => s.SnapshotDate.ToString("yyyy-MM")),
            AnalyticsTrendGranularity.Quarterly => snapshots.GroupBy(s =>
                $"{s.SnapshotDate.Year}-Q{(s.SnapshotDate.Month - 1) / 3 + 1}"),
            _ => snapshots.GroupBy(s => s.SnapshotDate.ToString("yyyy-MM-dd")),
        };

        return groups
            .Select(g => new TrendPointDto
            {
                Label = g.Key,
                Value = Math.Round(g.Average(selector), 2),
            })
            .ToList();
    }
}
