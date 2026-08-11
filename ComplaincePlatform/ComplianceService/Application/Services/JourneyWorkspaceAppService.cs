using System.Text.Json;
using ComplianceService.Application.Aggregation;
using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Http;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Maturity;
using ComplianceService.Application.Scoring;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public class JourneyWorkspaceAppService : IJourneyWorkspaceService
{
    private readonly ComplianceDbContext _db;
    private readonly IAggregationService _aggregation;
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IEvidenceService _evidenceService;
    private readonly IEvidenceRegistrationService _evidenceRegistration;
    private readonly IScoringService _scoringService;
    private readonly IOperationalModeClassificationPolicy _operationalModeClassification;
    private readonly IOperationalModeProgressionPolicy _operationalModeProgression;
    private readonly IFileStorageService _fileStorageService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IComplianceTaskEnsureService _complianceTaskEnsure;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotRecalculationQueue;
    private readonly IRequestContextCache _requestContextCache;
    private readonly IFrameworkCatalogCache _frameworkCatalogCache;
    private readonly IComplianceScoreService _complianceScore;
    private readonly IObligationNextActionService _nextActionService;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly ILogger<JourneyWorkspaceAppService> _logger;
    private readonly EvidenceOptions _evidenceOptions;

    public JourneyWorkspaceAppService(
        ComplianceDbContext db,
        IAggregationService aggregation,
        IComplianceJourneyRepository journeyRepository,
        IEvidenceService evidenceService,
        IEvidenceRegistrationService evidenceRegistration,
        IScoringService scoringService,
        IOperationalModeClassificationPolicy operationalModeClassification,
        IOperationalModeProgressionPolicy operationalModeProgression,
        IFileStorageService fileStorageService,
        IHttpContextAccessor httpContextAccessor,
        IComplianceTaskEnsureService complianceTaskEnsure,
        IComplianceSnapshotRecalculationQueue snapshotRecalculationQueue,
        IRequestContextCache requestContextCache,
        IFrameworkCatalogCache frameworkCatalogCache,
        IComplianceScoreService complianceScore,
        IObligationNextActionService nextActionService,
        IStageUnlockingService stageUnlocking,
        IOptions<EvidenceOptions> evidenceOptions,
        ILogger<JourneyWorkspaceAppService> logger)
    {
        _db = db;
        _aggregation = aggregation;
        _journeyRepository = journeyRepository;
        _evidenceService = evidenceService;
        _evidenceRegistration = evidenceRegistration;
        _scoringService = scoringService;
        _operationalModeClassification = operationalModeClassification;
        _operationalModeProgression = operationalModeProgression;
        _fileStorageService = fileStorageService;
        _httpContextAccessor = httpContextAccessor;
        _complianceTaskEnsure = complianceTaskEnsure;
        _snapshotRecalculationQueue = snapshotRecalculationQueue;
        _requestContextCache = requestContextCache;
        _frameworkCatalogCache = frameworkCatalogCache;
        _complianceScore = complianceScore;
        _nextActionService = nextActionService;
        _stageUnlocking = stageUnlocking;
        _evidenceOptions = evidenceOptions.Value;
        _logger = logger;
    }

    private void EnqueueComplianceSnapshotRecalc(ComplianceJourney journey)
    {
        _snapshotRecalculationQueue.EnqueueHospitalFramework(journey.HospitalId, journey.FrameworkId);
    }

    public async Task<JourneyWorkspaceSnapshotDto> GetWorkspaceAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        CancellationToken cancellationToken = default)
    {
        var ctx = await BuildWorkspaceContextAsync(journeyId, tenantId, operationalMode, null, cancellationToken);
        var trends = await JourneyTrendBuilder.BuildAsync(
            _db, ctx.Journey.HospitalId, ctx.Journey.FrameworkId, ct: cancellationToken);
        var oeIds = ctx.ObjectiveElements.Select(o => o.Id);
        var obligationByOeId = await LoadObligationsByOeIdAsync(ctx.Journey.HospitalId, oeIds, cancellationToken);
        return MapToWorkspaceSnapshot(ctx, trends, obligationByOeId);
    }

    public async Task<JourneySessionDto> GetSessionAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default)
    {
        var ctx = await BuildWorkspaceContextAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, cancellationToken);
        var j = ctx.Journey;
        var f = ctx.Framework;
        var readOnly = _operationalModeProgression.IsWorkspaceReadOnly(j, ctx.ResolvedMode);
        var snapReadOnly = _operationalModeProgression.IsSnapshotReadOnly(j);

        var allowed = new List<int>();
        if ((int)ctx.ResolvedMode < (int)OperationalMode.ReAccreditation)
        {
            allowed.Add((int)ctx.ResolvedMode + 1);
        }

        var locks = new Dictionary<string, string?>();
        for (var m = 0; m <= 4; m++)
        {
            if (m == (int)ctx.ResolvedMode + 1 && !_operationalModeProgression.CanChangeOperationalMode(j, out var reason))
            {
                locks[m.ToString()] = reason;
            }
            else if (m != (int)ctx.ResolvedMode + 1 && m > (int)ctx.ResolvedMode)
            {
                locks[m.ToString()] = "Complete prior operational phases in order.";
            }
            else
            {
                locks[m.ToString()] = null;
            }
        }

        return new JourneySessionDto
        {
            JourneyId = journeyId,
            FrameworkCode = f.PlatformId ?? f.Name,
            FrameworkName = $"{f.Name} {f.Version}".Trim(),
            OperationalMode = (int)ctx.ResolvedMode,
            ReadOnly = readOnly,
            SnapshotReadOnly = snapReadOnly,
            LastSyncedAt = DateTime.UtcNow,
            Catalog = new JourneyCatalogMetaDto
            {
                StandardEdition = f.Version,
                EffectiveFrom = null,
                TotalStandardsExpected = ctx.Standards.Count,
                TotalOesExpected = ctx.ObjectiveElements.Count,
                IsPartialCatalog = false
            },
            ModeProgression = new JourneyModeProgressionDto
            {
                AllowedNextModes = allowed,
                LockReasonByTargetMode = locks
            }
        };
    }

    public async Task<JourneyDashboardDto> GetDashboardAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default)
    {
        var ctx = await BuildWorkspaceContextAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, cancellationToken);
        var trends = await JourneyTrendBuilder.BuildAsync(
            _db, ctx.Journey.HospitalId, ctx.Journey.FrameworkId, ct: cancellationToken);
        var oeIds = ctx.ObjectiveElements.Select(o => o.Id);
        var obligationByOeId = await LoadObligationsByOeIdAsync(ctx.Journey.HospitalId, oeIds, cancellationToken);
        var overview = BuildOverview(ctx.ObjectiveElements, ctx.LatestByOe, ctx.ActionPlans, obligationByOeId);
        var scoreStates = ctx.LatestByOe.ToDictionary(
            kv => kv.Key,
            kv => new ObjectiveElementScoreState(kv.Value.Score, kv.Value.IsApplicable));
        var elig = NabhAccreditationEvaluator.Evaluate(ctx.Aggregate, scoreStates, ctx.OeById);
        var acc = MapDashboardAccreditation(ctx.Aggregate);
        acc.Violations = elig.Violations;
        // Headline readiness comes from the canonical single source of truth so the journey
        // workspace headline matches Dashboard, Sidebar, Journey-details and Reports. (The
        // detailed Accreditation breakdown still uses the workspace classification policy.)
        var readiness = await _complianceScore.GetJourneyScoreAsync(
            ctx.Journey.FrameworkId, ctx.Journey.Id, cancellationToken);

        var (coreScore, commitmentScore, achievementScore, excellenceScore) =
            MaturityStagePresentationMapper.ScoresFromStageBreakdown(overview.StageBreakdown);

        IReadOnlyList<StageUnlockEvaluationResult> stageEvaluations;
        try
        {
            stageEvaluations = await _stageUnlocking.GetStageEvaluationsAsync(
                ctx.Journey.HospitalId,
                ctx.Journey.OrganizationId,
                ctx.Journey.FrameworkId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Stage unlock evaluation failed for journey {JourneyId}; using score fallback",
                journeyId);
            stageEvaluations = Array.Empty<StageUnlockEvaluationResult>();
        }

        var maturityStages = MaturityStagePresentationMapper.BuildStages(
            stageEvaluations, coreScore, commitmentScore, achievementScore, excellenceScore);
        var currentStage = MaturityStagePresentationMapper.DetermineCurrentStage(
            stageEvaluations, coreScore, commitmentScore, achievementScore);

        return new JourneyDashboardDto
        {
            Overview = overview,
            Accreditation = acc,
            AccreditationReadinessPercent = readiness,
            OeScoreTrend = trends.OverallScoreTrend,
            ChapterImprovementTrend = trends.ChapterImprovementTrend,
            RiskEvolutionTrend = trends.RiskEvolutionTrend,
            CurrentStage = currentStage,
            MaturityStages = maturityStages
        };
    }

    public async Task<JourneyChaptersIndexDto> GetChaptersIndexAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        string? search,
        string? sort,
        string? classificationFilter = null,
        CancellationToken cancellationToken = default)
    {
        var ctx = await BuildWorkspaceContextAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, cancellationToken);
        var items = MapToChapterIndexItems(ctx);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            items = items.Where(i =>
                    i.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || i.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Filter chapters to only those containing OEs of the requested maturity stage.
        if (!string.IsNullOrWhiteSpace(classificationFilter)
            && Enum.TryParse<ObjectiveElementClassification>(classificationFilter, ignoreCase: true, out var filterClassification))
        {
            // Build set of chapter IDs that have at least one OE with the requested classification.
            var chapterIdsWithStage = new HashSet<Guid>(
                ctx.ObjectiveElements
                    .Where(oe => oe.Classification == filterClassification)
                    .Join(ctx.Standards, oe => oe.ComplianceStandardId, s => s.Id, (oe, s) => s.ChapterId)
                    .Distinct());

            items = items.Where(i => chapterIdsWithStage.Contains(i.Id)).ToList();
        }

        if (string.Equals(sort, "risk", StringComparison.OrdinalIgnoreCase))
        {
            items = items.OrderByDescending(i => i.Risk == "high" ? 3 : i.Risk == "medium" ? 2 : 1).ToList();
        }
        else if (string.Equals(sort, "code", StringComparison.OrdinalIgnoreCase))
        {
            items = items.OrderBy(i => i.Code).ToList();
        }

        return new JourneyChaptersIndexDto { Items = items };
    }

    public async Task<ObjectiveElementDto> GetObjectiveElementDetailAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default)
    {
        var ctx = await BuildWorkspaceContextAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, cancellationToken);
        if (!ctx.OeById.TryGetValue(objectiveElementId, out var oe))
        {
            throw new InvalidOperationException("Objective element not found for this journey.");
        }

        ctx.LatestByOe.TryGetValue(objectiveElementId, out var latest);
        var dto = BuildObjectiveElementDto(oe, latest, ctx);
        dto.StandardId = oe.ComplianceStandardId;
        dto.Evidence = await GetObjectiveElementEvidenceAsync(journeyId, objectiveElementId, tenantId, cancellationToken);
        dto.History = await GetObjectiveElementHistoryAsync(journeyId, objectiveElementId, tenantId, cancellationToken);
        dto.ActionPlans = await LoadActionPlansForOeAsync(objectiveElementId, cancellationToken);
        ApplyPhase48ObjectiveElement(dto, ctx.ResolvedMode, ctx.Journey, ctx.OeWizardStatusByObjectiveElementId);
        dto.Version = await _db.ObjectiveElementScores.AsNoTracking()
            .CountAsync(s => s.JourneyId == journeyId && s.ObjectiveElementId == objectiveElementId, cancellationToken);
        return dto;
    }

    private async Task<JourneyWorkspaceContext> BuildWorkspaceContextAsync(
        Guid journeyId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        var mode = OperationalModeQuery.ResolveForJourney(journey, operationalMode, auditLifecycleMode);
        var cacheKey = BuildWorkspaceCacheKey(journeyId, tenantId, mode);
        if (_requestContextCache.TryGet<JourneyWorkspaceContext>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var built = await BuildWorkspaceContextCoreAsync(journey, mode, cancellationToken);
        _requestContextCache.Set(cacheKey, built);
        return built;
    }

    private async Task<JourneyWorkspaceContext> BuildWorkspaceContextCoreAsync(
        ComplianceJourney journey,
        OperationalMode mode,
        CancellationToken cancellationToken)
    {
        var framework = await _db.ComplianceFrameworks.AsNoTracking()
            .FirstAsync(f => f.Id == journey.FrameworkId, cancellationToken);
        var included = _operationalModeClassification.GetClassificationsForWorkspace(mode).ToArray();
        var modeKey = BuildCatalogModeKey(included);
        var catalog = await _frameworkCatalogCache.GetOrLoadAsync(
            journey.FrameworkId,
            modeKey,
            async ct =>
            {
                var chaptersRows = await _db.ComplianceChapters.AsNoTracking()
                    .Where(c => c.FrameworkId == journey.FrameworkId)
                    .OrderBy(c => c.Sequence)
                    .ToListAsync(ct);

                var chapterIdsRows = chaptersRows.Select(c => c.Id).ToList();
                var standardsRows = await _db.ComplianceStandards.AsNoTracking()
                    .Where(s => chapterIdsRows.Contains(s.ChapterId))
                    .OrderBy(s => s.Sequence)
                    .ToListAsync(ct);

                var standardIdsRows = standardsRows.Select(s => s.Id).ToList();
                var oeRows = await _db.ObjectiveElements.AsNoTracking()
                    .Where(oe => standardIdsRows.Contains(oe.ComplianceStandardId)
                                 && included.Contains(oe.Classification))
                    .ToListAsync(ct);

                return new FrameworkCatalogSnapshot
                {
                    FrameworkId = journey.FrameworkId,
                    Chapters = chaptersRows,
                    Standards = standardsRows,
                    ObjectiveElements = oeRows
                };
            },
            cancellationToken);

        var chapters = catalog.Chapters.ToList();
        var standards = catalog.Standards.ToList();
        var objectiveElements = catalog.ObjectiveElements.ToList();

        var aggregate = await _aggregation.ComputeFrameworkAsync(
            journey.FrameworkId,
            journey.Id,
            chapters,
            standards,
            objectiveElements,
            cancellationToken);

        var oeIds = objectiveElements.Select(o => o.Id).ToList();
        var oeById = objectiveElements.ToDictionary(o => o.Id);

        var scoreRows = await _db.ObjectiveElementScores.AsNoTracking()
            .Where(s => s.JourneyId == journey.Id && oeIds.Contains(s.ObjectiveElementId))
            .OrderByDescending(s => s.AssessedAt)
            .ToListAsync(cancellationToken);

        var latestByOe = scoreRows
            .GroupBy(s => s.ObjectiveElementId)
            .ToDictionary(g => g.Key, g => g.First());

        _ = NabhAccreditationEvaluator.Evaluate(
            aggregate,
            latestByOe.ToDictionary(kv => kv.Key, kv => new ObjectiveElementScoreState(kv.Value.Score, kv.Value.IsApplicable)),
            oeById);

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.Scope == FindingScope.Audit && f.ComplianceJourneyId == journey.Id)
            .ToListAsync(cancellationToken);

        var observations = await _db.ObjectiveElementObservations.AsNoTracking()
            .Where(o => o.ComplianceJourneyId == journey.Id)
            .ToListAsync(cancellationToken);

        var evidenceLinks = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => oeIds.Contains(l.ObjectiveElementId))
            .ToListAsync(cancellationToken);

        var actionPlans = await _db.ActionPlans.AsNoTracking()
            .Where(a => oeIds.Contains(a.ObjectiveElementId)
                        && (a.Status == ActionPlanStatus.Open || a.Status == ActionPlanStatus.InProgress))
            .ToListAsync(cancellationToken);

        var wizardByOe = await LoadOeWizardStatusesAsync(journey.Id, cancellationToken);

        return new JourneyWorkspaceContext
        {
            Journey = journey,
            Framework = framework,
            ResolvedMode = mode,
            Aggregate = aggregate,
            Chapters = chapters,
            Standards = standards,
            ObjectiveElements = objectiveElements,
            LatestByOe = latestByOe,
            Findings = findings,
            Observations = observations,
            EvidenceLinks = evidenceLinks,
            ActionPlans = actionPlans,
            OeById = oeById,
            OeWizardStatusByObjectiveElementId = wizardByOe
        };
    }

    private async Task<IReadOnlyDictionary<Guid, ObjectiveJourneyStatus>> LoadOeWizardStatusesAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ObjectiveJourneys.AsNoTracking()
            .Where(x => x.ComplianceJourneyId == complianceJourneyId)
            .Select(x => new { x.ObjectiveElementId, x.Status })
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.ObjectiveElementId)
            .ToDictionary(g => g.Key, g => g.First().Status);
    }

    private static string MapWizardStatusToJourneyWorkflowLabel(ObjectiveJourneyStatus status) =>
        status switch
        {
            ObjectiveJourneyStatus.Completed => "completed",
            ObjectiveJourneyStatus.InProgress => "in_progress",
            _ => "not_started"
        };

    private static string BuildWorkspaceCacheKey(Guid journeyId, Guid tenantId, OperationalMode mode)
        => $"workspace:{journeyId:N}:tenant:{tenantId:N}:mode:{(int)mode}";

    private static string BuildCatalogModeKey(IReadOnlyCollection<ObjectiveElementClassification> included)
        => string.Join("-", included
            .Select(x => (int)x)
            .OrderBy(x => x));

    private JourneyWorkspaceSnapshotDto MapToWorkspaceSnapshot(
        JourneyWorkspaceContext ctx,
        JourneyTrendBuilder.JourneyTrendSeries trends,
        IReadOnlyDictionary<Guid, OrganizationComplianceObligation>? obligationByOeId = null)
    {
        var journey = ctx.Journey;
        var framework = ctx.Framework;
        var chapterDtos = MapToChapterWithStandardsDtos(ctx, obligationByOeId);
        var overview = BuildOverview(ctx.ObjectiveElements, ctx.LatestByOe, ctx.ActionPlans, obligationByOeId);
        var scoreStates = ctx.LatestByOe.ToDictionary(
            kv => kv.Key,
            kv => new ObjectiveElementScoreState(kv.Value.Score, kv.Value.IsApplicable));
        var accreditation = NabhAccreditationEvaluator.Evaluate(ctx.Aggregate, scoreStates, ctx.OeById);

        return new JourneyWorkspaceSnapshotDto
        {
            JourneyId = journey.Id,
            FrameworkCode = framework.Name,
            FrameworkName = $"{framework.Name} {framework.Version}".Trim(),
            OperationalMode = ctx.ResolvedMode,
            Chapters = chapterDtos,
            Accreditation = accreditation,
            Overview = overview,
            OeScoreTrend = trends.OverallScoreTrend,
            ChapterImprovementTrend = trends.ChapterImprovementTrend,
            RiskEvolutionTrend = trends.RiskEvolutionTrend
        };
    }

    private async Task<IReadOnlyDictionary<Guid, OrganizationComplianceObligation>> LoadObligationsByOeIdAsync(
        Guid hospitalId,
        IEnumerable<Guid> oeIds,
        CancellationToken ct)
    {
        var idList = oeIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, OrganizationComplianceObligation>();

        var obligations = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .Where(o => o.HospitalId == hospitalId && idList.Contains(o.ObjectiveElementId))
            .ToListAsync(ct);

        return obligations.GroupBy(o => o.ObjectiveElementId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private ObligationStateDto MapObligationState(OrganizationComplianceObligation obl)
    {
        var next = _nextActionService.GetNextAction(
            obl.AssessmentStatus,
            obl.ImplementationStatus,
            obl.JourneyStatus,
            obl.EvidenceStatus,
            obl.VerificationStatus);

        return new ObligationStateDto
        {
            ObligationId = obl.Id,
            ImplementationStatus = obl.ImplementationStatus.ToString(),
            AssessmentStatus = obl.AssessmentStatus.ToString(),
            JourneyStatus = obl.JourneyStatus.ToString(),
            EvidenceStatus = obl.EvidenceStatus.ToString(),
            VerificationStatus = obl.VerificationStatus.ToString(),
            ReadinessScore = ObligationReadinessCalculator.CalculateObligationReadiness(
                obl.AssessmentStatus, obl.ImplementationStatus, obl.EvidenceStatus, obl.VerificationStatus),
            LastAssessmentDate = obl.LastAssessmentDate,
            LastVerificationDate = obl.LastVerificationDate,
            LastSnapshotRefreshDate = obl.LastSnapshotRefreshDate,
            NextActionLabel = next.Label,
            NextActionKey = next.ActionKey.ToString(),
            IsNextActionUrgent = next.IsUrgent
        };
    }

    private List<ChapterWithStandardsDto> MapToChapterWithStandardsDtos(
        JourneyWorkspaceContext ctx,
        IReadOnlyDictionary<Guid, OrganizationComplianceObligation>? obligationByOeId = null)
    {
        var journey = ctx.Journey;
        var aggregate = ctx.Aggregate;
        var chapters = ctx.Chapters;
        var standards = ctx.Standards;
        var objectiveElements = ctx.ObjectiveElements;
        var latestByOe = ctx.LatestByOe;
        var findings = ctx.Findings;
        var observations = ctx.Observations;
        var evidenceLinks = ctx.EvidenceLinks;

        var standardsByChapter = standards.GroupBy(s => s.ChapterId).ToDictionary(g => g.Key, g => g.ToList());
        var oesByStandard = objectiveElements.GroupBy(o => o.ComplianceStandardId).ToDictionary(g => g.Key, g => g.ToList());

        var chapterDtos = new List<ChapterWithStandardsDto>();
        foreach (var ch in chapters)
        {
            var stList = standardsByChapter.GetValueOrDefault(ch.Id, new List<ComplianceStandard>());
            var chapterStandardIds = stList.Select(s => s.Id).ToHashSet();
            var chapterOes = objectiveElements.Where(o => chapterStandardIds.Contains(o.ComplianceStandardId)).ToList();
            var chFailedCore = chapterOes.Count(oe =>
                oe.Classification == ObjectiveElementClassification.Core
                && (!latestByOe.TryGetValue(oe.Id, out var ls)
                    || !ls.IsApplicable
                    || ls.Score < 4));

            var stdDtos = new List<ComplianceStandardDto>();
            foreach (var std in stList)
            {
                var oeList = oesByStandard.GetValueOrDefault(std.Id, new List<ObjectiveElement>());
                var oeDtos = new List<ObjectiveElementDto>();
                var naCount = 0;
                foreach (var oe in oeList)
                {
                    latestByOe.TryGetValue(oe.Id, out var latest);
                    if (latest != null && !latest.IsApplicable)
                    {
                        naCount++;
                    }

                    var dto = BuildObjectiveElementDto(oe, latest, ctx);
                    dto.StandardId = std.Id;
                    if (obligationByOeId != null && obligationByOeId.TryGetValue(oe.Id, out var obl))
                    {
                        dto.ObligationState = MapObligationState(obl);
                    }
                    ApplyPhase48ObjectiveElement(
                        dto,
                        ctx.ResolvedMode,
                        ctx.Journey,
                        ctx.OeWizardStatusByObjectiveElementId);
                    oeDtos.Add(dto);
                }

                var aggStd = aggregate.Chapters
                    .FirstOrDefault(c => c.ChapterId == ch.Id)?.Standards
                    .FirstOrDefault(s => s.ComplianceStandardId == std.Id);

                var openIssues = findings.Count(f =>
                    !f.IsResolved
                    && f.ObjectiveElementId.HasValue
                    && oeList.Exists(o => o.Id == f.ObjectiveElementId.Value));
                stdDtos.Add(new ComplianceStandardDto
                {
                    Id = std.Id,
                    ChapterId = std.ChapterId,
                    Code = std.Code,
                    Name = std.Name,
                    Description = null,
                    Sequence = std.Sequence,
                    Weight = std.Weight,
                    ObjectiveElements = oeDtos,
                    AverageScore = aggStd?.AverageScore,
                    CoreOeAverage = ComputeCoreAverage(oeList, latestByOe),
                    NaCount = naCount,
                    CompletionPercent = aggStd?.CompliancePercent,
                    RiskBadge = RiskFromAverage(aggStd?.AverageScore ?? 0),
                    CoreSummary = null,
                    OpenIssuesCount = openIssues,
                    ComplianceStatusLabel = aggStd?.CompliancePercent >= 80m ? "Met" : "At risk",
                    EvidenceGapLabel = openIssues > 0 ? $"{openIssues} gaps" : null,
                    ChangeTrend = "flat",
                    PreviouslyFailed = false,
                    ExcellenceBadge = oeList.Any(o => o.Classification == ObjectiveElementClassification.Excellence
                        && latestByOe.TryGetValue(o.Id, out var s) && s.IsApplicable && s.Score >= 4)
                });
            }

            var chAgg = aggregate.Chapters.FirstOrDefault(c => c.ChapterId == ch.Id);
            var chapterOeIdSet = chapterOes.Select(o => o.Id).ToHashSet();

            var chapterObligations = obligationByOeId == null
                ? Enumerable.Empty<OrganizationComplianceObligation>()
                : chapterOes
                    .Where(o => obligationByOeId.ContainsKey(o.Id))
                    .Select(o => obligationByOeId[o.Id]);
            var chapterObligationReadiness = obligationByOeId != null
                ? ObligationReadinessCalculator.CalculateJourneyReadiness(chapterObligations)
                : 0m;

            chapterDtos.Add(new ChapterWithStandardsDto
            {
                Id = ch.Id,
                Code = ch.Code,
                Name = ch.Name,
                Sequence = ch.Sequence,
                Weight = ch.Weight,
                Standards = stdDtos,
                ComplianceScorePercent = chAgg?.CompliancePercent,
                Risk = RiskFromAverage(chAgg?.AverageScore ?? 0),
                CoreComplianceStatus = CoreStatusFromFailedCore(
                    chFailedCore,
                    chapterOes.Count(o => o.Classification == ObjectiveElementClassification.Core)),
                OpenIssuesCount = findings.Count(f =>
                    !f.IsResolved
                    && f.ObjectiveElementId.HasValue
                    && chapterOeIdSet.Contains(f.ObjectiveElementId.Value)),
                PendingAudit = journey.Status == ComplianceLifecycleState.AuditScheduled,
                CoreFailureFlag = chFailedCore > 0,
                AverageScore = chAgg?.AverageScore,
                FailedCoreOeCount = chFailedCore,
                ObligationReadinessScore = chapterObligationReadiness
            });
        }

        return chapterDtos;
    }

    private List<JourneyChapterIndexItemDto> MapToChapterIndexItems(JourneyWorkspaceContext ctx)
    {
        var list = new List<JourneyChapterIndexItemDto>();
        foreach (var ch in ctx.Chapters)
        {
            var chAgg = ctx.Aggregate.Chapters.FirstOrDefault(c => c.ChapterId == ch.Id);
            var stList = ctx.Standards.Where(s => s.ChapterId == ch.Id).ToList();
            var chapterOes = ctx.ObjectiveElements.Where(o => stList.Any(s => s.Id == o.ComplianceStandardId)).ToList();
            var chFailedCore = chapterOes.Count(oe =>
                oe.Classification == ObjectiveElementClassification.Core
                && (!ctx.LatestByOe.TryGetValue(oe.Id, out var ls)
                    || !ls.IsApplicable
                    || ls.Score < 4));
            var oeIdsInChapter = chapterOes.Select(o => o.Id).ToHashSet();
            var issueCount = ctx.Findings.Count(f =>
                !f.IsResolved
                && f.ObjectiveElementId.HasValue
                && oeIdsInChapter.Contains(f.ObjectiveElementId.Value));
            var mode = ctx.ResolvedMode;
            list.Add(new JourneyChapterIndexItemDto
            {
                Id = ch.Id,
                Code = ch.Code,
                Name = ch.Name,
                ComplianceScorePercent = chAgg?.CompliancePercent,
                Risk = RiskFromAverage(chAgg?.AverageScore ?? 0),
                CoreComplianceStatus = CoreStatusFromFailedCore(
                    chFailedCore,
                    chapterOes.Count(o => o.Classification == ObjectiveElementClassification.Core)),
                OpenIssuesCount = issueCount,
                PendingAudit = ctx.Journey.Status == ComplianceLifecycleState.AuditScheduled,
                CoreFailureFlag = chFailedCore > 0,
                IssueBadgeLabel = issueCount > 0 ? issueCount.ToString() : "—",
                PassFailLabel = mode == OperationalMode.FinalAudit
                    ? (chAgg?.AverageScore >= 4m ? "PASS" : "FAIL")
                    : null,
                ChangedSinceLastAudit = mode == OperationalMode.Surveillance ? null : null,
                MaturityLabel = mode == OperationalMode.ReAccreditation ? "Maturity L3" : null,
                HighlightHighRisk = mode == OperationalMode.InternalAudit && RiskFromAverage(chAgg?.AverageScore ?? 0) == "high"
            });
        }

        return list;
    }

    private static JourneyDashboardAccreditationDto MapDashboardAccreditation(FrameworkAggregateResult aggregate)
    {
        var chAvgs = aggregate.Chapters.Select(c => c.AverageScore).Where(a => a > 0).ToList();
        var stAvgs = aggregate.Chapters.SelectMany(c => c.Standards).Select(s => s.AverageScore).Where(a => a > 0).ToList();
        return new JourneyDashboardAccreditationDto
        {
            OverallCompliancePercent = aggregate.CompliancePercent,
            CoreOeAverageScore = aggregate.AverageScore,
            AverageChapterScore = chAvgs.Count == 0 ? 0 : Math.Round(chAvgs.Average(), 2),
            AverageStandardScore = stAvgs.Count == 0 ? 0 : Math.Round(stAvgs.Average(), 2),
            Violations = Array.Empty<string>()
        };
    }

    private ObjectiveElementDto BuildObjectiveElementDto(
        ObjectiveElement oe,
        ObjectiveElementScore? latest,
        JourneyWorkspaceContext ctx)
    {
        var dto = JourneyWorkspaceMapper.MapBase(oe);
        JourneyWorkspaceMapper.ApplyScore(dto, latest, oe);
        var linksForOe = ctx.EvidenceLinks.Where(l => l.ObjectiveElementId == oe.Id).ToList();
        var hasAny = linksForOe.Count > 0;
        var hasVerified = linksForOe.Any(l => l.VerificationStatus == EvidenceVerificationStatus.Verified);
        JourneyWorkspaceMapper.ApplyEvidenceStatus(dto, hasAny, hasVerified);
        dto.ObservationList = BuildObservationList(oe.Id, ctx.Findings, ctx.Observations);
        dto.AuditRemarks = latest?.Rationale;
        dto.DepartmentLabel = oe.Department;
        dto.LastUpdatedAt = latest?.AssessedAt;
        dto.Version = null;
        return dto;
    }

    private void ApplyPhase48ObjectiveElement(
        ObjectiveElementDto dto,
        OperationalMode mode,
        ComplianceJourney journey,
        IReadOnlyDictionary<Guid, ObjectiveJourneyStatus> wizardByOe)
    {
        if (wizardByOe.TryGetValue(dto.Id, out var st))
        {
            dto.JourneyWorkflowStatus = MapWizardStatusToJourneyWorkflowLabel(st);
        }
        else
        {
            dto.JourneyWorkflowStatus = "not_started";
        }

        dto.ChangedSinceLastAudit = mode == OperationalMode.Surveillance ? false : null;
        dto.ExcellenceNote = mode == OperationalMode.ReAccreditation ? null : null;
    }

    private async Task<IReadOnlyList<OeActionPlanDto>> LoadActionPlansForOeAsync(Guid objectiveElementId, CancellationToken cancellationToken)
    {
        var plans = await _db.ActionPlans.AsNoTracking()
            .Where(a => a.ObjectiveElementId == objectiveElementId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
        return plans.Select(p => new OeActionPlanDto
        {
            Id = p.Id,
            Title = string.IsNullOrWhiteSpace(p.CorrectiveAction) ? p.RootCause : p.CorrectiveAction,
            OwnerName = p.OwnerId.ToString(),
            Deadline = p.DueDate,
            Status = p.Status.ToString().ToLowerInvariant()
        }).ToList();
    }

    private static JourneyOverviewMetricsDto BuildOverview(
        List<ObjectiveElement> oes,
        Dictionary<Guid, ObjectiveElementScore> latestByOe,
        List<ActionPlan> openPlans,
        IReadOnlyDictionary<Guid, OrganizationComplianceObligation>? obligationByOeId = null)
    {
        var failedCore = 0;
        var atOrBelowThree = 0;

        // Per-stage accumulators for StageBreakdown
        var stageSums   = new Dictionary<ObjectiveElementClassification, (decimal Sum, int Denom, int Total)>();
        foreach (ObjectiveElementClassification c in Enum.GetValues<ObjectiveElementClassification>())
            stageSums[c] = (0m, 0, 0);

        foreach (var oe in oes)
        {
            var cls = ResolveMaturityBand(oe, obligationByOeId);
            var (sum, denom, total) = stageSums[cls];
            stageSums[cls] = (sum, denom, total + 1);

            if (!latestByOe.TryGetValue(oe.Id, out var sc) || !sc.IsApplicable)
                continue;

            if (sc.Score <= 3) atOrBelowThree++;

            var (s2, d2, t2) = stageSums[cls];
            stageSums[cls] = (s2 + sc.Score, d2 + 1, t2);

            if (cls == ObjectiveElementClassification.Core && sc.Score < 4)
                failedCore++;
        }

        var (coreSum, coreDenom, _) = stageSums[ObjectiveElementClassification.Core];
        var corePct = coreDenom == 0 ? 0m : Math.Round((coreSum / coreDenom) / 5m * 100m, 2);

        var allObligations = obligationByOeId?.Values.ToList() ?? new List<OrganizationComplianceObligation>();

        var stageBreakdown = new List<StageProgressDto>
        {
            BuildStageProgress(ObjectiveElementClassification.Core,       "Core",       stageSums, allObligations),
            BuildStageProgress(ObjectiveElementClassification.Commitment,  "Commitment", stageSums, allObligations),
            BuildStageProgress(ObjectiveElementClassification.Achievement, "Achievement",stageSums, allObligations),
            BuildStageProgress(ObjectiveElementClassification.Excellence,  "Excellence", stageSums, allObligations),
        };

        ObligationMaturityBreakdownDto? breakdown = null;
        var journeyReadiness = 0m;
        if (obligationByOeId != null && allObligations.Count > 0)
        {
            breakdown = BuildObligationMaturityBreakdown(allObligations);
            journeyReadiness = ObligationReadinessCalculator.CalculateJourneyReadiness(allObligations);
        }

        return new JourneyOverviewMetricsDto
        {
            CoreCompliancePercent = corePct,
            FailedCoreCount = failedCore,
            OesAtOrBelowThreeCount = atOrBelowThree,
            OpenActionPlansCount = openPlans.Count,
            StageBreakdown = stageBreakdown,
            ObligationBreakdown = breakdown,
            ObligationReadinessScore = journeyReadiness,
        };
    }

    private static ObligationMaturityBreakdownDto BuildObligationMaturityBreakdown(
        List<OrganizationComplianceObligation> obligations)
    {
        return new ObligationMaturityBreakdownDto
        {
            Core = BuildBandSummary(obligations, ObjectiveElementClassification.Core),
            Commitment = BuildBandSummary(obligations, ObjectiveElementClassification.Commitment),
            Achievement = BuildBandSummary(obligations, ObjectiveElementClassification.Achievement),
            Excellence = BuildBandSummary(obligations, ObjectiveElementClassification.Excellence),
        };
    }

    private static ObligationBandSummaryDto BuildBandSummary(
        List<OrganizationComplianceObligation> obligations,
        ObjectiveElementClassification stage)
        => ObligationBandSummaryCalculator.BuildBand(obligations, stage);

    private static StageProgressDto BuildStageProgress(
        ObjectiveElementClassification cls,
        string label,
        Dictionary<ObjectiveElementClassification, (decimal Sum, int Denom, int Total)> stageSums,
        List<OrganizationComplianceObligation>? allObligations = null)
    {
        var (sum, denom, total) = stageSums[cls];
        var score = denom == 0 ? 0m : Math.Round((sum / denom) / 5m * 100m, 2);

        var stageObs = allObligations?.Where(o => o.MaturityLevel == cls).ToList()
            ?? new List<OrganizationComplianceObligation>();

        var (verified, implemented, partial, deficient) =
            ObligationBandSummaryCalculator.MapStageCounts(stageObs);

        var useObligationTotals = allObligations is { Count: > 0 };

        return new StageProgressDto
        {
            Stage = cls.ToString().ToLowerInvariant(),
            Label = label,
            Score = score,
            TotalOeCount = useObligationTotals ? stageObs.Count : total,
            ScoredOeCount = denom,
            TotalObligations = stageObs.Count,
            VerifiedObligations = verified,
            ImplementedObligations = implemented,
            PartialObligations = partial,
            DeficientObligations = deficient,
            ObligationReadinessScore = ObligationReadinessCalculator.CalculateStageReadiness(
                stageObs, cls),
        };
    }

    /// <summary>
    /// Uses synced obligation maturity when available so stage metrics stay aligned with
    /// obligation summaries even if the framework catalog cache has stale OE classifications.
    /// </summary>
    private static ObjectiveElementClassification ResolveMaturityBand(
        ObjectiveElement oe,
        IReadOnlyDictionary<Guid, OrganizationComplianceObligation>? obligationByOeId)
    {
        if (obligationByOeId != null
            && obligationByOeId.TryGetValue(oe.Id, out var obligation))
        {
            return obligation.MaturityLevel;
        }

        return oe.Classification;
    }

    private static decimal? ComputeCoreAverage(
        List<ObjectiveElement> oeList,
        Dictionary<Guid, ObjectiveElementScore> latestByOe)
    {
        var cores = oeList.Where(o => o.Classification == ObjectiveElementClassification.Core).ToList();
        var scores = new List<int>();
        foreach (var oe in cores)
        {
            if (latestByOe.TryGetValue(oe.Id, out var sc) && sc.IsApplicable)
            {
                scores.Add(sc.Score);
            }
        }

        return scores.Count == 0 ? null : Math.Round((decimal)scores.Average(), 2);
    }

    private static (decimal? AverageScore, decimal? CompletionPercent) ComputeStandardAverages(
        List<ObjectiveElement> oeList,
        Dictionary<Guid, ObjectiveElementScore> latestByOe)
    {
        var included = new List<(decimal Weight, int Score)>();
        foreach (var oe in oeList)
        {
            if (!latestByOe.TryGetValue(oe.Id, out var score) || !score.IsApplicable)
            {
                continue;
            }

            included.Add((oe.Weight <= 0 ? 1.0m : oe.Weight, score.Score));
        }

        if (included.Count == 0)
        {
            return (null, null);
        }

        decimal weightedSum = 0m;
        decimal weightTotal = 0m;
        foreach (var (weight, score) in included)
        {
            weightedSum += weight * score;
            weightTotal += weight;
        }

        if (weightTotal <= 0m)
        {
            return (0m, 0m);
        }

        var avg = Math.Round(weightedSum / weightTotal, 2);
        var pct = Math.Round((avg / 5m) * 100m, 2);
        return (avg, pct);
    }

    private static string RiskFromAverage(decimal avg)
    {
        if (avg >= 4m)
        {
            return "low";
        }

        return avg >= 3m ? "medium" : "high";
    }

    private static string CoreStatusFromFailedCore(int failed, int coreCount)
    {
        if (coreCount == 0 || failed == 0)
        {
            return "met";
        }

        return failed <= 2 ? "at_risk" : "failed";
    }

    private static IReadOnlyList<OeObservationDto> BuildObservationList(
        Guid oeId,
        List<Finding> findings,
        List<ObjectiveElementObservation> observations)
    {
        var list = new List<OeObservationDto>();
        foreach (var f in findings.Where(x => x.ObjectiveElementId == oeId))
        {
            var sev = f.Type == Shared.Infrastructure.Domain.Enums.NonConformanceType.MajorNC ? "major" : "minor";
            list.Add(new OeObservationDto
            {
                Id = f.Id,
                Severity = sev,
                Text = f.Description,
                NonConformanceId = f.Id
            });
        }

        foreach (var o in observations.Where(x => x.ObjectiveElementId == oeId))
        {
            list.Add(new OeObservationDto
            {
                Id = o.Id,
                Severity = o.Severity,
                Text = o.Text,
                NonConformanceId = null
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<ComplianceStandardDto>> GetChapterStandardsAsync(
        Guid journeyId,
        Guid chapterId,
        Guid tenantId,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        int page,
        int pageSize,
        string? classificationFilter = null,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        var mode = OperationalModeQuery.ResolveForJourney(journey, operationalMode, auditLifecycleMode);
        var included = _operationalModeClassification.GetClassificationsForWorkspace(mode).ToArray();

        var chapter = await _db.ComplianceChapters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chapterId && c.FrameworkId == journey.FrameworkId, cancellationToken);
        if (chapter is null)
        {
            throw new InvalidOperationException($"Chapter {chapterId} not found in journey.");
        }

        var p = Math.Max(1, page);
        var ps = Math.Max(1, pageSize);
        var skip = (p - 1) * ps;

        var standards = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => s.ChapterId == chapterId)
            .OrderBy(s => s.Sequence)
            .Skip(skip)
            .Take(ps)
            .ToListAsync(cancellationToken);
        if (standards.Count == 0)
        {
            return Array.Empty<ComplianceStandardDto>();
        }

        var standardIds = standards.Select(s => s.Id).ToList();

        // Optionally narrow OEs to a specific maturity stage classification.
        ObjectiveElementClassification? stageClassification = null;
        if (!string.IsNullOrWhiteSpace(classificationFilter)
            && Enum.TryParse<ObjectiveElementClassification>(classificationFilter, ignoreCase: true, out var parsedStage))
        {
            stageClassification = parsedStage;
        }

        var objectiveElements = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => standardIds.Contains(oe.ComplianceStandardId)
                      && included.Contains(oe.Classification)
                      && (stageClassification == null || oe.Classification == stageClassification))
            .ToListAsync(cancellationToken);
        var oeIds = objectiveElements.Select(oe => oe.Id).ToList();

        var latestByOe = await _db.ObjectiveElementScores.AsNoTracking()
            .Where(s => s.JourneyId == journeyId && oeIds.Contains(s.ObjectiveElementId))
            .OrderByDescending(s => s.AssessedAt)
            .GroupBy(s => s.ObjectiveElementId)
            .Select(g => g.First())
            .ToDictionaryAsync(s => s.ObjectiveElementId, cancellationToken);

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.Scope == FindingScope.Audit
                && f.ComplianceJourneyId == journeyId
                && f.ObjectiveElementId.HasValue
                && oeIds.Contains(f.ObjectiveElementId.Value))
            .ToListAsync(cancellationToken);

        var observations = await _db.ObjectiveElementObservations.AsNoTracking()
            .Where(o => o.ComplianceJourneyId == journeyId && oeIds.Contains(o.ObjectiveElementId))
            .ToListAsync(cancellationToken);

        var evidenceLinks = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(e => oeIds.Contains(e.ObjectiveElementId))
            .ToListAsync(cancellationToken);

        var framework = await _db.ComplianceFrameworks.AsNoTracking()
            .FirstAsync(f => f.Id == journey.FrameworkId, cancellationToken);

        var wizardByOe = await LoadOeWizardStatusesAsync(journeyId, cancellationToken);

        var localContext = new JourneyWorkspaceContext
        {
            Journey = journey,
            Framework = framework,
            ResolvedMode = mode,
            Aggregate = new FrameworkAggregateResult { FrameworkId = journey.FrameworkId },
            Chapters = new List<ComplianceChapter> { chapter },
            Standards = standards,
            ObjectiveElements = objectiveElements,
            LatestByOe = latestByOe,
            Findings = findings,
            Observations = observations,
            EvidenceLinks = evidenceLinks,
            ActionPlans = new List<ActionPlan>(),
            OeById = objectiveElements.ToDictionary(o => o.Id),
            OeWizardStatusByObjectiveElementId = wizardByOe
        };

        var oeByStandard = objectiveElements
            .GroupBy(oe => oe.ComplianceStandardId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<ComplianceStandardDto>(standards.Count);
        foreach (var std in standards)
        {
            var oeList = oeByStandard.GetValueOrDefault(std.Id, new List<ObjectiveElement>());
            var oeDtos = new List<ObjectiveElementDto>(oeList.Count);
            var naCount = 0;
            foreach (var oe in oeList)
            {
                latestByOe.TryGetValue(oe.Id, out var latest);
                if (latest is not null && !latest.IsApplicable)
                {
                    naCount++;
                }

                var dto = BuildObjectiveElementDto(oe, latest, localContext);
                dto.StandardId = std.Id;
                ApplyPhase48ObjectiveElement(
                    dto,
                    localContext.ResolvedMode,
                    localContext.Journey,
                    localContext.OeWizardStatusByObjectiveElementId);
                oeDtos.Add(dto);
            }

            var openIssues = findings.Count(f =>
                !f.IsResolved
                && f.ObjectiveElementId.HasValue
                && oeList.Any(o => o.Id == f.ObjectiveElementId.Value));
            var (averageScore, completionPercent) = ComputeStandardAverages(oeList, latestByOe);
            rows.Add(new ComplianceStandardDto
            {
                Id = std.Id,
                ChapterId = std.ChapterId,
                Code = std.Code,
                Name = std.Name,
                Description = null,
                Sequence = std.Sequence,
                Weight = std.Weight,
                ObjectiveElements = oeDtos,
                AverageScore = averageScore,
                CoreOeAverage = ComputeCoreAverage(oeList, latestByOe),
                NaCount = naCount,
                CompletionPercent = completionPercent,
                RiskBadge = RiskFromAverage(averageScore ?? 0m),
                CoreSummary = null,
                OpenIssuesCount = openIssues,
                ComplianceStatusLabel = completionPercent >= 80m ? "Met" : "At risk",
                EvidenceGapLabel = openIssues > 0 ? $"{openIssues} gaps" : null,
                ChangeTrend = "flat",
                PreviouslyFailed = false,
                ExcellenceBadge = oeList.Any(o => o.Classification == ObjectiveElementClassification.Excellence
                    && latestByOe.TryGetValue(o.Id, out var score) && score.IsApplicable && score.Score >= 4)
            });
        }

        return rows;
    }

    public async Task<PatchObjectiveElementResponseDto> PatchObjectiveElementAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        IReadOnlyList<string> roles,
        PatchObjectiveElementRequest request,
        OperationalMode? operationalMode,
        string? auditLifecycleMode,
        CancellationToken cancellationToken = default)
    {
        _ = roles;
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        if (journey.Status >= ComplianceLifecycleState.Submitted)
        {
            throw new InvalidOperationException("Journey is locked for objective element updates after submission.");
        }

        var oe = await _db.ObjectiveElements.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == objectiveElementId, cancellationToken)
            ?? throw new InvalidOperationException("Objective element not found.");

        var std = await _db.ComplianceStandards.AsNoTracking()
            .FirstAsync(s => s.Id == oe.ComplianceStandardId, cancellationToken);
        var chapter = await _db.ComplianceChapters.AsNoTracking()
            .FirstAsync(c => c.Id == std.ChapterId, cancellationToken);
        if (chapter.FrameworkId != journey.FrameworkId)
        {
            throw new InvalidOperationException("Objective element does not belong to this journey's framework.");
        }

        var mode = OperationalModeQuery.ResolveForJourney(journey, operationalMode, auditLifecycleMode);
        if (!_operationalModeClassification.IsClassificationEditable(mode, oe.Classification))
        {
            throw new InvalidOperationException("Objective element classification is not editable in this operational mode.");
        }

        if (request.Version is int expectedVersion)
        {
            var currentVersion = await _db.ObjectiveElementScores.AsNoTracking()
                .CountAsync(
                    s => s.JourneyId == journeyId && s.ObjectiveElementId == objectiveElementId,
                    cancellationToken);
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException("VERSION_CONFLICT");
            }
        }

        if (request.IsNa == true && string.IsNullOrWhiteSpace(request.NaJustification))
        {
            throw new InvalidOperationException("NA justification is required when marking an objective element as not applicable.");
        }

        if (request.Score is int sc)
        {
            if (sc < 1 || sc > 5)
            {
                throw new InvalidOperationException("Score must be between 1 and 5.");
            }

            var hasVerified = await _db.ObjectiveElementEvidences.AsNoTracking()
                .AnyAsync(
                    l => l.ObjectiveElementId == objectiveElementId
                         && l.VerificationStatus == EvidenceVerificationStatus.Verified,
                    cancellationToken);

            if (sc == 5 && !hasVerified)
            {
                throw new InvalidOperationException("Score 5 requires at least one verified evidence item for this objective element.");
            }

            await _scoringService.SetScoreAsync(
                objectiveElementId,
                journeyId,
                userId,
                sc,
                request.ScoreRationale ?? string.Empty,
                evidenceSampleCompliancePercent: 100m,
                isDocumentationInadequate: false,
                downgradeReason: null,
                cancellationToken);
        }

        if (request.IsNa == true)
        {
            await _scoringService.SetNAAsync(
                objectiveElementId,
                journeyId,
                isApplicable: false,
                naReason: request.NaJustification,
                naApprovedBy: userId,
                cancellationToken);
        }
        else if (request.IsNa == false)
        {
            await _scoringService.SetNAAsync(
                objectiveElementId,
                journeyId,
                isApplicable: true,
                naReason: null,
                naApprovedBy: null,
                cancellationToken);
        }

        var detail = await GetObjectiveElementDetailAsync(
            journeyId,
            objectiveElementId,
            tenantId,
            operationalMode,
            auditLifecycleMode,
            cancellationToken);
        var ctxAfter = await BuildWorkspaceContextAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, cancellationToken);
        var overview = BuildOverview(ctxAfter.ObjectiveElements, ctxAfter.LatestByOe, ctxAfter.ActionPlans);
        return new PatchObjectiveElementResponseDto
        {
            ObjectiveElement = detail,
            Overview = overview,
            Version = detail.Version ?? 0
        };
    }

    public async Task<JourneySessionDto> PatchOperationalModeAsync(
        Guid journeyId,
        Guid tenantId,
        int operationalMode,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        if (!_operationalModeProgression.CanChangeOperationalMode(journey, out var deny))
        {
            throw new InvalidOperationException(deny ?? "Operational mode cannot be changed.");
        }

        if (operationalMode < 0 || operationalMode > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(operationalMode));
        }

        var target = (OperationalMode)operationalMode;
        journey.SetOperationalMode(target);
        await _journeyRepository.UpdateAsync(journey, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetSessionAsync(journeyId, tenantId, null, null, cancellationToken);
    }

    public async Task DeleteObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        if (journey.Status >= ComplianceLifecycleState.Submitted)
        {
            throw new InvalidOperationException("Evidence is locked after submission of the compliance journey.");
        }

        await _evidenceService.RemoveEvidenceFromObjectiveElementAsync(objectiveElementId, evidenceId, cancellationToken);
    }

    public async Task<IReadOnlyList<OeEvidenceItemDto>> GetObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureJourneyTenantAsync(journeyId, tenantId, cancellationToken);
        var list = await _evidenceService.GetEvidenceForObjectiveElementAsync(objectiveElementId, cancellationToken: cancellationToken);
        var links = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(l => l.ObjectiveElementId == objectiveElementId)
            .ToListAsync(cancellationToken);

        return list.Select(ev =>
        {
            var link = links.FirstOrDefault(l => l.EvidenceId == ev.Id);
            return new OeEvidenceItemDto
            {
                Id = ev.Id,
                Name = ev.Title ?? ev.EvidenceType,
                VerificationStatus = link?.VerificationStatus.ToString() ?? "Unknown",
                UploadedAt = ev.UploadedOn,
                ExpiryAt = ev.ExpiresAt,
                UploadedBy = ev.UploadedBy
            };
        }).ToList();
    }

    public async Task<Guid> RegisterObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        RegisterOeEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _evidenceRegistration.RegisterFromUrlAsync(
            new EvidenceRegistrationUrlRequest
            {
                Context = EvidenceRegistrationContext.ForJourneyObjectiveElement(
                    journeyId,
                    objectiveElementId,
                    tenantId,
                    userId),
                EvidenceType = request.EvidenceType,
                FileUrl = request.FileUrl,
                Title = request.Title,
                Metadata = request.Metadata
            },
            cancellationToken);

        return result.EvidenceId;
    }

    public async Task<Guid> RegisterObjectiveElementEvidenceFromUploadAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        string evidenceType,
        string? displayName = null,
        string? title = null,
        string? metadata = null,
        CancellationToken cancellationToken = default,
        string? metadataJsonOverride = null,
        bool suppressSnapshotRecalc = false)
    {
        var result = await _evidenceRegistration.RegisterFromUploadAsync(
            new EvidenceRegistrationUploadRequest
            {
                Context = EvidenceRegistrationContext.ForJourneyObjectiveElement(
                    journeyId,
                    objectiveElementId,
                    tenantId,
                    userId),
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType,
                EvidenceType = evidenceType,
                DisplayName = displayName,
                Title = title,
                Metadata = metadata,
                Options = new EvidenceRegistrationOptions
                {
                    SuppressSnapshotRecalc = suppressSnapshotRecalc,
                    MetadataJsonOverride = metadataJsonOverride
                }
            },
            cancellationToken);

        return result.EvidenceId;
    }

    public async Task<IReadOnlyList<OeHistoryEventDto>> GetObjectiveElementHistoryAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureJourneyTenantAsync(journeyId, tenantId, cancellationToken);

        var scores = await _db.ObjectiveElementScores.AsNoTracking()
            .Where(s => s.JourneyId == journeyId && s.ObjectiveElementId == objectiveElementId)
            .OrderByDescending(s => s.AssessedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var events = scores.Select(s => new OeHistoryEventDto
        {
            Id = s.Id,
            Type = "score",
            Title = s.IsApplicable ? $"Score set to {s.Score}" : "Marked NA",
            Description = s.Rationale,
            Timestamp = s.AssessedAt,
            Actor = s.AssessorId.ToString()
        }).ToList();

        return events;
    }

    private async Task EnsureJourneyTenantAsync(Guid journeyId, Guid tenantId, CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }
    }

    private static string BuildEvidenceMetadataJson(string? contentType, string originalFileName, string? userMetadata)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["contentType"] = contentType ?? "application/octet-stream",
            ["originalFileName"] = originalFileName
        };
        if (!string.IsNullOrWhiteSpace(userMetadata))
        {
            payload["userMetadata"] = userMetadata;
        }

        return JsonSerializer.Serialize(payload);
    }

    public async Task<EvidenceDetailDto?> GetObjectiveElementEvidenceDetailAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureJourneyTenantAsync(journeyId, tenantId, cancellationToken);
        await EnsureEvidenceBelongsToObjectiveElementAsync(journeyId, objectiveElementId, evidenceId, cancellationToken);
        return await _evidenceService.GetEvidenceDetailAsync(objectiveElementId, evidenceId, cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType, string FileDownloadName)> DownloadObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureJourneyTenantAsync(journeyId, tenantId, cancellationToken);
        var evidence = await EnsureEvidenceBelongsToObjectiveElementAsync(journeyId, objectiveElementId, evidenceId, cancellationToken);

        _logger.LogInformation(
            "Evidence download requested for journey {JourneyId} OE {ObjectiveElementId} evidence {EvidenceId} correlation {CorrelationId}",
            journeyId,
            objectiveElementId,
            evidenceId,
            CorrelationId());

        var stream = await _fileStorageService.OpenReadAsync(evidence.FileUrl, cancellationToken);
        TryGetDownloadHeaders(evidence.Metadata, evidence.Title, evidenceId, out var contentType, out var fileName);
        return (stream, contentType, fileName);
    }

    public async Task VerifyObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        Guid userId,
        bool accepted,
        string? remarks,
        CancellationToken cancellationToken = default)
    {
        await EnsureJourneyTenantAsync(journeyId, tenantId, cancellationToken);
        await EnsureJourneyWritableForEvidenceAsync(journeyId, cancellationToken);
        await EnsureEvidenceBelongsToObjectiveElementAsync(journeyId, objectiveElementId, evidenceId, cancellationToken);

        _logger.LogInformation(
            "Evidence verify/reject for journey {JourneyId} OE {ObjectiveElementId} evidence {EvidenceId} accepted={Accepted} correlation {CorrelationId}",
            journeyId,
            objectiveElementId,
            evidenceId,
            accepted,
            CorrelationId());

        await _evidenceService.SetVerificationAsync(objectiveElementId, evidenceId, userId, accepted, remarks, cancellationToken);
    }

    public async Task<Guid> UploadObjectiveElementEvidenceVersionAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        Guid tenantId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        await EnsureJourneyWritableForEvidenceAsync(journeyId, cancellationToken);
        var evidence = await EnsureEvidenceBelongsToObjectiveElementAsync(journeyId, objectiveElementId, evidenceId, cancellationToken);

        var safeRelative = Path.Combine(
            "App_Data",
            "evidence_uploads",
            journeyId.ToString("N"),
            objectiveElementId.ToString("N"),
            evidenceId.ToString("N"),
            "versions");

        var safeFile = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "upload.bin" : fileName);
        var destName = $"{Guid.NewGuid():N}_{safeFile}";

        _logger.LogInformation(
            "Saving evidence version file under {StoragePath} journey {JourneyId} evidence {EvidenceId} correlation {CorrelationId}",
            safeRelative,
            journeyId,
            evidenceId,
            CorrelationId());

        var fileUrl = await _fileStorageService.SaveAsync(
            fileStream,
            destName,
            contentType ?? "application/octet-stream",
            safeRelative,
            cancellationToken);

        await _evidenceService.AppendEvidenceVersionAsync(evidence.Id, fileUrl, userId, notes, cancellationToken);

        _logger.LogInformation(
            "Evidence version uploaded for evidence {EvidenceId} on journey {JourneyId} correlation {CorrelationId}",
            evidenceId,
            journeyId,
            CorrelationId());

        EnqueueComplianceSnapshotRecalc(journey);
        return evidenceId;
    }

    public async Task<BulkEvidenceUploadResponseDto> BulkUploadObjectiveElementEvidenceAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        IReadOnlyList<BulkUploadIncomingFile> files,
        string evidenceType,
        string? sharedMetadata,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            throw new ArgumentException("Evidence type is required.", nameof(evidenceType));
        }

        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException($"Access denied to journey {journeyId}.");
        }

        if (journey.Status >= ComplianceLifecycleState.Submitted)
        {
            throw new InvalidOperationException("Evidence is locked after submission of the compliance journey.");
        }

        if (files.Count > _evidenceOptions.BulkUploadMaxFiles)
        {
            throw new InvalidOperationException($"Maximum {_evidenceOptions.BulkUploadMaxFiles} files per bulk upload request.");
        }

        var items = new List<BulkEvidenceUploadItemResult>();
        var success = 0;
        var fail = 0;

        foreach (var incoming in files)
        {
            var name = string.IsNullOrWhiteSpace(incoming.FileName) ? "upload.bin" : incoming.FileName;
            if (incoming.Length <= 0)
            {
                items.Add(new BulkEvidenceUploadItemResult
                {
                    FileName = name,
                    Status = "Failed",
                    ErrorMessage = "Empty file."
                });
                fail++;
                continue;
            }

            if (incoming.Length > _evidenceOptions.MaxFileBytes)
            {
                items.Add(new BulkEvidenceUploadItemResult
                {
                    FileName = name,
                    Status = "Failed",
                    ErrorMessage = $"File exceeds maximum size of {_evidenceOptions.MaxFileBytes} bytes."
                });
                fail++;
                continue;
            }

            try
            {
                var metaJson = BuildEvidenceMetadataJson(incoming.ContentType, Path.GetFileName(name), sharedMetadata);
                var id = await RegisterObjectiveElementEvidenceFromUploadAsync(
                    journeyId,
                    objectiveElementId,
                    tenantId,
                    userId,
                    incoming.Stream,
                    name,
                    incoming.ContentType,
                    evidenceType,
                    displayName: null,
                    title: Path.GetFileNameWithoutExtension(name),
                    metadata: null,
                    cancellationToken,
                    metadataJsonOverride: metaJson,
                    suppressSnapshotRecalc: true);
                items.Add(new BulkEvidenceUploadItemResult
                {
                    FileName = name,
                    Status = "Success",
                    EvidenceId = id
                });
                success++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Bulk evidence item failed for file {FileName} journey {JourneyId} OE {ObjectiveElementId} correlation {CorrelationId}",
                    name,
                    journeyId,
                    objectiveElementId,
                    CorrelationId());
                items.Add(new BulkEvidenceUploadItemResult
                {
                    FileName = name,
                    Status = "Failed",
                    ErrorMessage = ex.Message
                });
                fail++;
            }
        }

        _logger.LogInformation(
            "Bulk evidence upload for journey {JourneyId} OE {ObjectiveElementId}: total={Total} success={Success} failure={Failure} correlation {CorrelationId}",
            journeyId,
            objectiveElementId,
            files.Count,
            success,
            fail,
            CorrelationId());

        if (success > 0)
        {
            EnqueueComplianceSnapshotRecalc(journey);
        }

        return new BulkEvidenceUploadResponseDto
        {
            TotalFiles = files.Count,
            SuccessCount = success,
            FailureCount = fail,
            Items = items
        };
    }

    private async Task EnsureJourneyWritableForEvidenceAsync(Guid journeyId, CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");
        if (journey.Status >= ComplianceLifecycleState.Submitted)
        {
            throw new InvalidOperationException("Evidence is locked after submission of the compliance journey.");
        }
    }

    private async Task<Evidence> EnsureEvidenceBelongsToObjectiveElementAsync(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken)
            ?? throw new InvalidOperationException($"Compliance journey {journeyId} not found.");

        var link = await _db.ObjectiveElementEvidences.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ObjectiveElementId == objectiveElementId && x.EvidenceId == evidenceId,
                cancellationToken);
        if (link == null)
        {
            throw new InvalidOperationException("Evidence is not linked to this objective element.");
        }

        var task = await _db.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.HospitalId == journey.HospitalId && t.ObjectiveElementId == objectiveElementId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (task == null)
        {
            throw new InvalidOperationException("No compliance task found for this objective element.");
        }

        var evidence = await _db.Evidences.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (evidence == null || evidence.TaskId != task.Id)
        {
            throw new InvalidOperationException("Evidence does not belong to this journey context.");
        }

        return evidence;
    }

    private static void TryGetDownloadHeaders(
        string? metadataJson,
        string? title,
        Guid evidenceId,
        out string contentType,
        out string fileDownloadName)
    {
        contentType = "application/octet-stream";
        fileDownloadName = $"{evidenceId:N}";
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                fileDownloadName = title!;
            }

            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("contentType", out var ct) && ct.ValueKind == JsonValueKind.String)
            {
                var s = ct.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    contentType = s!;
                }
            }

            if (root.TryGetProperty("originalFileName", out var on) && on.ValueKind == JsonValueKind.String)
            {
                var s = on.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    fileDownloadName = s!;
                    return;
                }
            }
        }
        catch
        {
            // fall through
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            fileDownloadName = title!;
        }
    }

    private string? CorrelationId() => CorrelationIdAccessor.FromHttpContext(_httpContextAccessor.HttpContext);
}
