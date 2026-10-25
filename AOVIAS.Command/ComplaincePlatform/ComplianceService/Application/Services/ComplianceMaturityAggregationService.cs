using ComplianceService.Application.Analytics;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Maturity;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;
using ComplianceScoreResult = ComplianceService.Application.ComplianceScoring.ComplianceScoreResult;
using GapSeverity = ComplianceService.Application.DTOs.GapSeverity;
using AppIComplianceScoringService = ComplianceService.Application.Contracts.IComplianceScoringService;
namespace ComplianceService.Application.Services;

/// <summary>
/// Orchestrates the assembly of the Compliance Maturity Dashboard.
///
/// Data flow:
///   1. Resolve active ComplianceJourney → framework + organization context.
///   2. Calculate hospital compliance score → core readiness score.
///   3. Derive CMI via <see cref="ComplianceMaturityIndexService"/>.
///   4. Generate predicted gaps via <see cref="PredictedGapEngineService"/>.
///   5. Build journey recommendations (rules-based top-5).
///   6. Assemble and return <see cref="ComplianceMaturityDashboardDto"/>.
///
/// Performance: uses AsNoTracking projections; never loads all 503 questions.
/// Target response time: &lt; 2 seconds.
/// </summary>
public sealed class ComplianceMaturityAggregationService : IComplianceMaturityAggregationService
{
    private readonly ComplianceDbContext _db;
    private readonly AppIComplianceScoringService _scoringService;
    private readonly IComplianceScoreService _stageScoreService;
    private readonly ComplianceMaturityIndexService _cmiService;
    private readonly PredictedGapEngineService _predEngine;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly IEscalationService _escalationService;
    private readonly ICapaQueryService _capaQuery;
    private readonly IAnalyticsAggregationService _analytics;
    private readonly ILogger<ComplianceMaturityAggregationService> _logger;

    public ComplianceMaturityAggregationService(
        ComplianceDbContext db,
        AppIComplianceScoringService scoringService,
        IComplianceScoreService stageScoreService,
        ComplianceMaturityIndexService cmiService,
        PredictedGapEngineService predEngine,
        IStageUnlockingService stageUnlocking,
        IEscalationService escalationService,
        ICapaQueryService capaQuery,
        IAnalyticsAggregationService analytics,
        ILogger<ComplianceMaturityAggregationService> logger)
    {
        _db = db;
        _scoringService = scoringService;
        _stageScoreService = stageScoreService;
        _cmiService = cmiService;
        _predEngine = predEngine;
        _stageUnlocking = stageUnlocking;
        _escalationService = escalationService;
        _capaQuery = capaQuery;
        _analytics = analytics;
        _logger = logger;
    }

    public async Task<ComplianceMaturityDashboardDto> GetDashboardAsync(
        Guid hospitalId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Building compliance maturity dashboard for HospitalId={HospitalId} OrgId={OrgId}",
            hospitalId, organizationId);

        // ── 1. Resolve active journey context ────────────────────────────────
        var journeyCtx = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.EnrollmentDate)
            .Select(j => new
            {
                j.Id,
                j.FrameworkId,
                j.OrganizationId,
                j.EnrollmentDate,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var frameworkId = journeyCtx?.FrameworkId ?? Guid.Empty;

        // Resolve framework name from master data
        var frameworkName = await ResolveFrameworkNameAsync(frameworkId, cancellationToken);

        // Resolve organization name
        var orgName = await ResolveOrganizationNameAsync(organizationId, cancellationToken);

        // ── 2. Score calculation ─────────────────────────────────────────────
        ComplianceScoreResult scoreResult;
        try
        {
            scoreResult = frameworkId != Guid.Empty
                ? await _scoringService.CalculateFrameworkScoreAsync(hospitalId, frameworkId)
                : await _scoringService.CalculateHospitalScoreAsync(hospitalId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Score calculation failed for HospitalId={HospitalId}; defaulting to zero", hospitalId);
            scoreResult = new ComplianceScoreResult { HospitalId = hospitalId };
        }

        var coreScore = scoreResult.OverallScore;

        // Compute real per-stage scores from live OE assessments.
        // Fall back to 0 when no OEs for that stage have been scored yet.
        var journeyId = journeyCtx?.Id ?? Guid.Empty;
        decimal commitmentScore  = 0m;
        decimal achievementScore = 0m;
        decimal excellenceScore  = 0m;
        if (journeyId != Guid.Empty)
        {
            try
            {
                commitmentScore = await _stageScoreService.GetStageScoreAsync(journeyId, OeClassification.Commitment, cancellationToken);
                achievementScore = await _stageScoreService.GetStageScoreAsync(journeyId, OeClassification.Achievement, cancellationToken);
                excellenceScore = await _stageScoreService.GetStageScoreAsync(journeyId, OeClassification.Excellence, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stage score calculation failed for JourneyId={JourneyId}; defaulting to zero", journeyId);
            }
        }

        // ── 3. CMI ────────────────────────────────────────────────────────────
        var cmi = _cmiService.Calculate(coreScore, commitmentScore, achievementScore, excellenceScore);

        // ── 4. Stage unlock engine (obligations + deficiencies + verified evidence) ─
        IReadOnlyList<StageUnlockEvaluationResult> stageEvaluations;
        try
        {
            stageEvaluations = await _stageUnlocking.GetStageEvaluationsAsync(
                hospitalId,
                organizationId,
                frameworkId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stage unlock evaluation failed; falling back to score-only model");
            stageEvaluations = Array.Empty<StageUnlockEvaluationResult>();
        }

        var currentStage = MaturityStagePresentationMapper.DetermineCurrentStage(
            stageEvaluations, coreScore, commitmentScore, achievementScore);
        var stages = MaturityStagePresentationMapper.BuildStages(
            stageEvaluations, coreScore, commitmentScore, achievementScore, excellenceScore);

        // ── 6. Build Core gap summary (from open tasks + score result) ────────
        var coreSummary = await BuildCoreGapSummaryAsync(hospitalId, frameworkId, scoreResult, cancellationToken);

        // ── 7. Predicted gaps ─────────────────────────────────────────────────
        var predictions = _predEngine.GeneratePredictions(coreScore, commitmentScore, achievementScore);

        // ── 8. Next actions ───────────────────────────────────────────────────
        var nextActions = BuildNextActions(currentStage, stages, coreScore, commitmentScore, scoreResult);

        var activeBlocking = BuildActiveStageBlocking(currentStage, stages, stageEvaluations, coreSummary);
        var forecast = BuildAccreditationForecast(cmi, stages, currentStage);
        var unlockTimeline = journeyId != Guid.Empty
            ? await LoadUnlockAuditTimelineAsync(journeyId, cancellationToken)
            : Array.Empty<UnlockAuditTimelineEntryDto>();

        var auditOriginCount = await _db.ComplianceDeficiencies
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(
                d => d.HospitalId == hospitalId
                     && d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled
                     && d.OriginType == DeficiencyOriginType.AuditFinding
                     && (frameworkId == Guid.Empty || d.FrameworkId == frameworkId),
                cancellationToken);

        var scope = AnalyticsScope.ForHospital(organizationId, hospitalId, frameworkId);
        var governance = await _analytics.GetGovernanceMetricsAsync(scope, cancellationToken);

        return new ComplianceMaturityDashboardDto
        {
            OrganizationId         = organizationId,
            OrganizationName       = orgName,
            FrameworkId            = frameworkId,
            FrameworkName          = frameworkName,
            AssessmentDate         = journeyCtx?.EnrollmentDate,
            ComplianceMaturityIndex = cmi,
            CurrentStage           = currentStage,
            Stages                 = stages,
            CoreSummary            = coreSummary,
            CommitmentSummary      = predictions.Commitment,
            AchievementSummary     = predictions.Achievement,
            ExcellenceSummary      = predictions.Excellence,
            NextActions            = nextActions,
            ActiveStageBlocking    = activeBlocking,
            AccreditationForecast  = forecast,
            UnlockAuditTimeline    = unlockTimeline,
            AuditOriginDeficiencyCount = auditOriginCount,
            SlaGovernance = governance.Sla,
            CapaGovernance = governance.Capa,
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> ResolveFrameworkNameAsync(Guid frameworkId, CancellationToken ct)
    {
        if (frameworkId == Guid.Empty) return "NABH";

        var name = await _db.ComplianceFrameworks
            .AsNoTracking()
            .Where(f => f.Id == frameworkId)
            .Select(f => f.Name)
            .FirstOrDefaultAsync(ct);

        return name ?? "NABH";
    }

    private static Task<string> ResolveOrganizationNameAsync(Guid organizationId, CancellationToken ct)
    {
        // Organization name is owned by OrganizationService and not cached in ComplianceService.
        // This will be resolved via OrganizationService integration in a future phase.
        _ = organizationId;
        _ = ct;
        return Task.FromResult("Hospital");
    }

    private async Task<GapSummaryDto> BuildCoreGapSummaryAsync(
        Guid hospitalId,
        Guid frameworkId,
        ComplianceScoreResult scoreResult,
        CancellationToken ct)
    {
        var openDeficienciesQuery = _db.ComplianceDeficiencies
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => d.HospitalId == hospitalId
                        && d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled);

        if (frameworkId != Guid.Empty)
        {
            openDeficienciesQuery = openDeficienciesQuery.Where(d => d.FrameworkId == frameworkId);
        }

        var openDeficiencies = await openDeficienciesQuery
            .OrderByDescending(d => (int)d.Severity)
            .ThenBy(d => d.DetectedDate)
            .Take(20)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.Description,
                d.Severity,
                d.Status,
                d.ObjectiveElementId
            })
            .ToListAsync(ct);

        var oeIds = openDeficiencies.Select(d => d.ObjectiveElementId).Distinct().ToList();
        var oeMeta = await (
            from oe in _db.ObjectiveElements.AsNoTracking()
            join s in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            where oeIds.Contains(oe.Id)
            select new
            {
                oe.Id,
                ChapterName = ch.Name,
                StandardTitle = s.Name
            }).ToDictionaryAsync(x => x.Id, x => x, ct);

        var gapItems = openDeficiencies.Select(d =>
        {
            oeMeta.TryGetValue(d.ObjectiveElementId, out var meta);
            return new GapItemDto
            {
                Id = d.Id.ToString(),
                Title = !string.IsNullOrWhiteSpace(d.Title) ? d.Title : "Compliance deficiency",
                Severity = MapDeficiencySeverityToGap(d.Severity),
                Chapter = meta?.ChapterName ?? "General",
                Standard = meta?.StandardTitle ?? string.Empty,
                Status = MapDeficiencyStatusToGap(d.Status),
                Classification = ComplianceItemClassification.ComplianceDeficiency
            };
        }).ToList();

        var openCount = await openDeficienciesQuery.CountAsync(ct);

        return new GapSummaryDto
        {
            ReadinessScore = scoreResult.OverallScore,
            OpenGapCount = openCount,
            CriticalCount = scoreResult.CriticalCount,
            MajorCount = scoreResult.MajorCount,
            MinorCount = scoreResult.MinorCount,
            Items = gapItems
        };
    }

    private static GapSeverity MapDeficiencySeverityToGap(DeficiencySeverity severity) =>
        severity switch
        {
            DeficiencySeverity.Critical => GapSeverity.Critical,
            DeficiencySeverity.Major    => GapSeverity.Major,
            DeficiencySeverity.Moderate => GapSeverity.Major,
            _                           => GapSeverity.Minor
        };

    private static GapItemStatus MapDeficiencyStatusToGap(DeficiencyStatus status) =>
        status is DeficiencyStatus.InProgress or DeficiencyStatus.EvidenceSubmitted
            or DeficiencyStatus.ReviewPending or DeficiencyStatus.Verified
            ? GapItemStatus.InRemediation
            : GapItemStatus.Open;

    private static IReadOnlyList<JourneyRecommendationDto> BuildNextActions(
        MaturityStage currentStage,
        IReadOnlyList<MaturityStageDto> stages,
        decimal coreScore,
        decimal commitmentScore,
        ComplianceScoreResult scoreResult)
    {
        var actions = new List<JourneyRecommendationDto>();
        var priority = 1;

        var lockedStage = stages.FirstOrDefault(s => s.Status == MaturityStageStatus.Locked);
        if (lockedStage?.Recommendations.Count > 0)
        {
            foreach (var rec in lockedStage.Recommendations.Take(2))
            {
                actions.Add(new JourneyRecommendationDto
                {
                    Title = $"To unlock {lockedStage.Label}",
                    Description = rec,
                    Priority = priority++,
                    ActionType = "StageUnlockRecommendation",
                    IsEnabled = true
                });
            }
        }

        if (scoreResult.CriticalCount > 0)
        {
            actions.Add(new JourneyRecommendationDto
            {
                Title = "Resolve Critical Non-Conformances",
                Description = $"You have {scoreResult.CriticalCount} critical non-conformance(s) blocking progression.",
                Priority = priority++,
                ActionType = "ReviewCriticalGaps",
                IsEnabled = true
            });
        }

        var active = stages.FirstOrDefault(s => s.Status is MaturityStageStatus.InProgress or MaturityStageStatus.Available);
        if (active?.NextMilestone != null)
        {
            actions.Add(new JourneyRecommendationDto
            {
                Title = $"{active.Label} — Next Milestone",
                Description = active.NextMilestone,
                Priority = priority++,
                ActionType = "ContinueCoreRemediation",
                IsEnabled = true
            });
        }
        else if (currentStage == MaturityStage.Core)
        {
            actions.Add(new JourneyRecommendationDto
            {
                Title = "Continue Core Remediation",
                Description = $"Core readiness is {coreScore:F0}%. Close deficiencies and verify evidence to unlock Commitment.",
                Priority = priority++,
                ActionType = "ContinueCoreRemediation",
                IsEnabled = true
            });
        }

        actions.Add(new JourneyRecommendationDto
        {
            Title = "View Full Gap Analysis",
            Description = "Review the complete gap analysis report for all objective elements.",
            Priority = priority,
            ActionType = "ViewFullGapAnalysis",
            IsEnabled = true
        });

        return actions.OrderBy(a => a.Priority).Take(5).ToList();
    }

    private static StageBlockingSummaryDto? BuildActiveStageBlocking(
        MaturityStage currentStage,
        IReadOnlyList<MaturityStageDto> stages,
        IReadOnlyList<StageUnlockEvaluationResult> evaluations,
        GapSummaryDto coreSummary)
    {
        var focusStage = stages.FirstOrDefault(s =>
            s.Status is MaturityStageStatus.InProgress or MaturityStageStatus.Available)
            ?? stages.FirstOrDefault(s => s.Status == MaturityStageStatus.Locked);
        if (focusStage == null) return null;

        var oeStage = MapMaturityStageToOe(focusStage.Stage);
        var eval = evaluations.FirstOrDefault(e => e.Stage == oeStage);
        var metrics = eval?.Metrics;
        var remainingObligations = metrics == null
            ? 0
            : Math.Max(0, metrics.TotalObligations - metrics.VerifiedObligationCount);
        var readinessGap = Math.Max(0m, focusStage.UnlockThreshold - focusStage.ReadinessPercent);

        return new StageBlockingSummaryDto
        {
            Stage = focusStage.Stage,
            StageLabel = focusStage.Label,
            OpenCriticalDeficiencies = metrics?.OpenCriticalDeficiencies ?? coreSummary.CriticalCount,
            MissingEvidenceCount = metrics?.MissingEvidenceCount ?? 0,
            VerificationFailureCount = metrics?.PendingVerificationCount ?? 0,
            ReadinessGapPercent = readinessGap,
            RemainingObligationsCount = remainingObligations,
            BlockingReasons = focusStage.BlockingReasons.Count > 0
                ? focusStage.BlockingReasons
                : eval?.BlockingReasons ?? Array.Empty<string>()
        };
    }

    private static AccreditationForecastDto BuildAccreditationForecast(
        ComplianceMaturityIndexDto cmi,
        IReadOnlyList<MaturityStageDto> stages,
        MaturityStage currentStage)
    {
        var active = stages.FirstOrDefault(s =>
            s.Status is MaturityStageStatus.InProgress or MaturityStageStatus.Available)
            ?? stages.FirstOrDefault(s => s.Stage == currentStage)
            ?? stages.FirstOrDefault();
        var currentReadiness = active?.ReadinessPercent ?? cmi.Value;
        var target = active?.UnlockThreshold ?? 100m;
        var estimatedUnlock = active?.UnlockReadinessPercent ?? active?.ProgressPercent ?? currentReadiness;
        var remaining = Math.Max(0m, 100m - estimatedUnlock);

        return new AccreditationForecastDto
        {
            CurrentReadinessPercent = currentReadiness,
            TargetReadinessPercent = target,
            EstimatedUnlockReadinessPercent = estimatedUnlock,
            RemainingEffortPercent = remaining,
            ComplianceMaturityIndex = cmi.Value
        };
    }

    private async Task<IReadOnlyList<UnlockAuditTimelineEntryDto>> LoadUnlockAuditTimelineAsync(
        Guid journeyId,
        CancellationToken ct)
    {
        var entries = await _db.MaturityStageUnlockAuditEntries
            .AsNoTracking()
            .Where(e => e.JourneyId == journeyId)
            .OrderByDescending(e => e.EvaluatedAt)
            .Take(25)
            .ToListAsync(ct);

        return entries.Select(MapUnlockAuditEntry).ToList();
    }

    private static UnlockAuditTimelineEntryDto MapUnlockAuditEntry(MaturityStageUnlockAuditEntry e)
    {
        var stageLabel = e.Stage.ToString();
        var (title, eventType) = e.NewState switch
        {
            MaturityStageLifecycleState.Completed => ($"{stageLabel} Completed", "StageCompleted"),
            MaturityStageLifecycleState.Available => ($"{stageLabel} Unlocked", "StageUnlocked"),
            MaturityStageLifecycleState.InProgress => ($"{stageLabel} In Progress", "StageProgress"),
            MaturityStageLifecycleState.Locked when e.PreviousState != MaturityStageLifecycleState.Locked
                => ($"{stageLabel} Blocked", "StageBlocked"),
            MaturityStageLifecycleState.Locked => ($"{stageLabel} Locked", "StageBlocked"),
            _ => ($"{stageLabel} Updated", "StageUpdated")
        };

        var description = !string.IsNullOrWhiteSpace(e.CriteriaFailedJson)
            ? $"Criteria review — source: {e.TriggerSource}"
            : $"Stage state {e.PreviousState} → {e.NewState}";

        if (e.NewState == MaturityStageLifecycleState.Completed)
        {
            description = "Readiness threshold achieved for stage completion.";
        }
        else if (e.NewState == MaturityStageLifecycleState.Available)
        {
            description = "Prerequisite stage complete — next stage is now available.";
        }

        return new UnlockAuditTimelineEntryDto
        {
            Title = title,
            Description = description,
            OccurredAt = e.EvaluatedAt,
            EventType = eventType
        };
    }

    private static OeClassification MapMaturityStageToOe(MaturityStage stage) =>
        stage switch
        {
            MaturityStage.Commitment => OeClassification.Commitment,
            MaturityStage.Achievement => OeClassification.Achievement,
            MaturityStage.Excellence => OeClassification.Excellence,
            _ => OeClassification.Core
        };
}
