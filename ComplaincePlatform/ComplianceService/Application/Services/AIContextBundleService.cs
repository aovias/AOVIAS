using System.Security.Cryptography;
using System.Text;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

public sealed class AIContextBundleService : IAIContextBundleService
{
    private readonly ComplianceDbContext _dbContext;
    private readonly IComplianceScoreService _scoreService;
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly IAIDeficiencyContextProvider _deficiencyProvider;
    private readonly IAICapaContextProvider _capaProvider;
    private readonly IAISlaContextProvider _slaProvider;
    private readonly IAIMaturityContextProvider _maturityProvider;
    private readonly IAIAnalyticsContextProvider _analyticsProvider;
    private readonly ILogger<AIContextBundleService> _logger;

    public AIContextBundleService(
        ComplianceDbContext dbContext,
        IComplianceScoreService scoreService,
        IAnalyticsQueryService analyticsQuery,
        IAIDeficiencyContextProvider deficiencyProvider,
        IAICapaContextProvider capaProvider,
        IAISlaContextProvider slaProvider,
        IAIMaturityContextProvider maturityProvider,
        IAIAnalyticsContextProvider analyticsProvider,
        ILogger<AIContextBundleService> logger)
    {
        _dbContext = dbContext;
        _scoreService = scoreService;
        _analyticsQuery = analyticsQuery;
        _deficiencyProvider = deficiencyProvider;
        _capaProvider = capaProvider;
        _slaProvider = slaProvider;
        _maturityProvider = maturityProvider;
        _analyticsProvider = analyticsProvider;
        _logger = logger;
    }

    public async Task<AIContextBundleDto> BuildAsync(AIContextBundleRequestDto request, CancellationToken cancellationToken)
    {
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AIContextSourceDto>();

        await AddOperationalContextAsync(request, facts, sources, cancellationToken);

        var organizationId = await ResolveOrganizationIdAsync(request, cancellationToken);
        if (organizationId != Guid.Empty)
        {
            try
            {
                var scope = await _analyticsQuery.ResolveScopeAsync(
                    organizationId, request.HospitalId, request.FrameworkId, cancellationToken);
                var buildContext = new AI.AIContextBuildContext { Request = request, Scope = scope };

                await MergeContributionAsync(_deficiencyProvider.BuildAsync(buildContext, cancellationToken), facts, sources);
                await MergeContributionAsync(_capaProvider.BuildAsync(buildContext, cancellationToken), facts, sources);
                await MergeContributionAsync(_slaProvider.BuildAsync(buildContext, cancellationToken), facts, sources);
                await MergeContributionAsync(_maturityProvider.BuildAsync(buildContext, cancellationToken), facts, sources);
                await MergeContributionAsync(_analyticsProvider.BuildAsync(buildContext, cancellationToken), facts, sources);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Workflow context enrichment failed for HospitalId={HospitalId}, FrameworkId={FrameworkId}",
                    request.HospitalId, request.FrameworkId);
            }
        }

        AddSessionContext(request, facts);

        var contextVersionId = BuildContextVersionId(request.HospitalId, sources);

        return new AIContextBundleDto
        {
            ContextVersionId = contextVersionId,
            Facts = facts,
            Sources = sources
                .GroupBy(x => $"{x.SourceType}:{x.SourceId}", StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(y => y.RelevanceScore).First())
                .OrderByDescending(x => x.RelevanceScore)
                .ToList()
        };
    }

    public async Task<AIContextVersionValidationResponseDto> ValidateVersionAsync(
        AIContextVersionValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        var bundle = await BuildAsync(new AIContextBundleRequestDto
        {
            HospitalId = request.HospitalId,
            ObjectiveElementId = request.ObjectiveElementId,
            TaskId = request.TaskId,
            EvidenceId = request.EvidenceId
        }, cancellationToken);

        return new AIContextVersionValidationResponseDto
        {
            IsCurrent = string.Equals(bundle.ContextVersionId, request.ContextVersionId, StringComparison.Ordinal),
            CurrentContextVersionId = bundle.ContextVersionId
        };
    }

    public async Task<AICommandPreconditionResponseDto> ValidateCommandPreconditionsAsync(
        AICommandPreconditionRequestDto request,
        CancellationToken cancellationToken)
    {
        var versionCheck = await ValidateVersionAsync(new AIContextVersionValidationRequestDto
        {
            HospitalId = request.HospitalId,
            ObjectiveElementId = request.ObjectiveElementId,
            TaskId = request.TaskId,
            EvidenceId = request.EvidenceId,
            ContextVersionId = request.ExpectedContextVersionId
        }, cancellationToken);

        if (!versionCheck.IsCurrent)
        {
            return new AICommandPreconditionResponseDto
            {
                IsAllowed = false,
                Reason = "Context version mismatch.",
                CurrentContextVersionId = versionCheck.CurrentContextVersionId
            };
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyToken))
        {
            return new AICommandPreconditionResponseDto
            {
                IsAllowed = false,
                Reason = "Missing idempotency token.",
                CurrentContextVersionId = versionCheck.CurrentContextVersionId
            };
        }

        if (!request.ApprovalAttested)
        {
            return new AICommandPreconditionResponseDto
            {
                IsAllowed = false,
                Reason = "Approval attestation required.",
                CurrentContextVersionId = versionCheck.CurrentContextVersionId
            };
        }

        return new AICommandPreconditionResponseDto
        {
            IsAllowed = true,
            Reason = "Preconditions satisfied.",
            CurrentContextVersionId = versionCheck.CurrentContextVersionId
        };
    }

    private async Task AddOperationalContextAsync(
        AIContextBundleRequestDto request,
        IDictionary<string, string> facts,
        ICollection<AIContextSourceDto> sources,
        CancellationToken cancellationToken)
    {
        var findingQuery = _dbContext.Findings
            .AsNoTracking()
            .Where(x => x.TenantId == request.HospitalId)
            .OrderByDescending(x => x.LastUpdatedAt)
            .Take(20);

        if (request.ObjectiveElementId.HasValue)
        {
            findingQuery = findingQuery.Where(x => x.ObjectiveElementId == request.ObjectiveElementId.Value);
        }

        var findings = await findingQuery.ToListAsync(cancellationToken);
        facts["findingCount"] = findings.Count.ToString();
        facts["openFindings"] = findings.Count(x => !x.IsResolved).ToString();

        foreach (var finding in findings.Take(5))
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "Finding",
                SourceId = finding.Id.ToString(),
                Summary = $"{finding.Title} ({finding.ComplianceStatus})",
                RelevanceScore = finding.IsMandatory ? 0.95m : 0.7m
            });
        }

        var taskQuery = _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(x => x.HospitalId == request.HospitalId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20);

        if (request.ObjectiveElementId.HasValue)
        {
            taskQuery = taskQuery.Where(x => x.ObjectiveElementId == request.ObjectiveElementId.Value);
        }

        var tasks = await taskQuery.ToListAsync(cancellationToken);
        facts["taskCount"] = tasks.Count.ToString();
        facts["pendingTaskCount"] = tasks.Count(x => x.Status != Domain.Enums.ComplianceTaskStatus.Completed).ToString();

        foreach (var task in tasks.Take(5))
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "Task",
                SourceId = task.Id.ToString(),
                Summary = $"{task.Title ?? task.Description ?? "Task"} ({task.Status})",
                RelevanceScore = task.Priority == Domain.Enums.ComplianceTaskPriority.High ? 0.9m : 0.65m
            });
        }

        var evidenceQuery = _dbContext.Evidences
            .AsNoTracking()
            .Where(x => x.HospitalId == request.HospitalId)
            .OrderByDescending(x => x.UploadedOn)
            .Take(20);

        if (request.TaskId.HasValue)
        {
            evidenceQuery = evidenceQuery.Where(x => x.TaskId == request.TaskId.Value);
        }

        var evidences = await evidenceQuery.ToListAsync(cancellationToken);
        facts["evidenceCount"] = evidences.Count.ToString();

        foreach (var evidence in evidences.Take(5))
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "Evidence",
                SourceId = evidence.Id.ToString(),
                Summary = $"{evidence.Title ?? evidence.EvidenceType} ({evidence.LifecycleStatus})",
                RelevanceScore = 0.6m
            });
        }

        if (request.ObjectiveElementId.HasValue)
        {
            var oe = await _dbContext.ObjectiveElements.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ObjectiveElementId.Value, cancellationToken);
            if (oe != null)
            {
                facts["objectiveElementCode"] = oe.Code;
                facts["objectiveElementMandatory"] = oe.IsMandatory.ToString();
                sources.Add(new AIContextSourceDto
                {
                    SourceType = "ObjectiveElement",
                    SourceId = oe.Id.ToString(),
                    Summary = $"{oe.Code} - {oe.Title}",
                    RelevanceScore = 1m
                });
            }
        }

        await AddReadinessContextAsync(request, facts, sources, cancellationToken);
    }

    private static void AddSessionContext(AIContextBundleRequestDto request, IDictionary<string, string> facts)
    {
        if (!string.IsNullOrWhiteSpace(request.PageContext))
        {
            facts["pageContext"] = request.PageContext.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ConversationSummary))
        {
            facts["conversationSummary"] = request.ConversationSummary.Trim();
        }

        if (request.RecentEntityRefs.Count > 0)
        {
            facts["recentEntityRefs"] = string.Join("; ", request.RecentEntityRefs.Take(8));
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType) && !string.IsNullOrWhiteSpace(request.EntityId))
        {
            facts["focusedEntity"] = $"{request.EntityType}:{request.EntityId}";
        }
    }

    private async Task<Guid> ResolveOrganizationIdAsync(AIContextBundleRequestDto request, CancellationToken cancellationToken)
    {
        if (request.OrganizationId is { } org && org != Guid.Empty)
        {
            return org;
        }

        var fromJourney = await _dbContext.ComplianceJourneys.AsNoTracking()
            .Where(j => j.HospitalId == request.HospitalId)
            .OrderByDescending(j => j.UpdatedAt)
            .Select(j => j.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        return fromJourney;
    }

    private static async Task MergeContributionAsync(
        Task<AI.AIContextContribution> task,
        IDictionary<string, string> facts,
        ICollection<AIContextSourceDto> sources)
    {
        var contribution = await task;
        foreach (var (key, value) in contribution.Facts)
        {
            facts[key] = value;
        }

        foreach (var source in contribution.Sources)
        {
            sources.Add(source);
        }
    }

    private async Task AddReadinessContextAsync(
        AIContextBundleRequestDto request,
        IDictionary<string, string> facts,
        ICollection<AIContextSourceDto> sources,
        CancellationToken cancellationToken)
    {
        try
        {
            var score = request.FrameworkId.HasValue
                ? await _scoreService.GetFrameworkScoreAsync(request.HospitalId, request.FrameworkId.Value, cancellationToken)
                : await _scoreService.GetHospitalScoreAsync(request.HospitalId, cancellationToken);

            if (score is null)
            {
                return;
            }

            facts["overallCompliancePercent"] = Math.Round(score.OverallScore, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            facts["auditConfidencePercent"] = Math.Round(score.AuditConfidenceScore, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            facts["criticalNonConformances"] = score.CriticalCount.ToString();
            facts["majorNonConformances"] = score.MajorCount.ToString();
            facts["minorNonConformances"] = score.MinorCount.ToString();
            facts["observationsCount"] = score.ObservationsCount.ToString();
            facts["opportunitiesForImprovement"] = score.OFICount.ToString();
            facts["scopedFramework"] = request.FrameworkId.HasValue ? request.FrameworkId.Value.ToString() : "all";

            var gapChapters = score.ChapterScores
                .OrderBy(x => x.CompletionPercentage)
                .Take(5)
                .ToList();

            if (gapChapters.Count == 0)
            {
                return;
            }

            var chapterIds = gapChapters.Select(x => x.ChapterId).ToList();
            var chapterNames = await _dbContext.ComplianceChapters
                .AsNoTracking()
                .Where(x => chapterIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => $"{x.Code} {x.Name}".Trim(), cancellationToken);

            var gapSummaries = new List<string>();
            foreach (var chapter in gapChapters)
            {
                var label = chapterNames.TryGetValue(chapter.ChapterId, out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : $"Chapter {chapter.ChapterId}";
                var completion = Math.Round(chapter.CompletionPercentage, 0);
                gapSummaries.Add($"{label} ({completion}% complete)");

                sources.Add(new AIContextSourceDto
                {
                    SourceType = "ChapterGap",
                    SourceId = chapter.ChapterId.ToString(),
                    Summary = $"{label}: {completion}% complete (weighted score {Math.Round(chapter.WeightedScore, 0)}%)",
                    RelevanceScore = Math.Clamp(1m - (chapter.CompletionPercentage / 100m), 0.5m, 0.99m)
                });
            }

            facts["topGapChapters"] = string.Join("; ", gapSummaries);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to enrich AI context bundle with readiness data. HospitalId={HospitalId}, FrameworkId={FrameworkId}",
                request.HospitalId,
                request.FrameworkId);
        }
    }

    private static string BuildContextVersionId(Guid hospitalId, IReadOnlyCollection<AIContextSourceDto> sources)
    {
        using var sha = SHA256.Create();
        var material = $"{hospitalId:N}|{string.Join("|", sources.Select(x => $"{x.SourceType}:{x.SourceId}"))}";
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash)[..24];
    }
}
