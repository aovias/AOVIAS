using System.Text.Json;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Application.Copilot.Templates;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Orchestrates the full draft generation pipeline:
/// template resolution → context assembly → content generation → persistence → versioning.
/// Does NOT create tasks, CAPAs, findings, or modify any compliance state.
/// </summary>
public sealed class DraftGenerationService : IDraftGenerationService
{
    private readonly IAiDraftRepository _draftRepository;
    private readonly ICopilotRecommendationRepository _recommendationRepository;
    private readonly DraftTemplateRegistry _templateRegistry;
    private readonly IDraftContentGenerator _contentGenerator;
    private readonly IAnalyticsAggregationService _analyticsAggregation;
    private readonly IDeficiencyMetricsService _deficiencyMetrics;
    private readonly ILogger<DraftGenerationService> _logger;

    public DraftGenerationService(
        IAiDraftRepository draftRepository,
        ICopilotRecommendationRepository recommendationRepository,
        DraftTemplateRegistry templateRegistry,
        IDraftContentGenerator contentGenerator,
        IAnalyticsAggregationService analyticsAggregation,
        IDeficiencyMetricsService deficiencyMetrics,
        ILogger<DraftGenerationService> logger)
    {
        _draftRepository         = draftRepository;
        _recommendationRepository = recommendationRepository;
        _templateRegistry        = templateRegistry;
        _contentGenerator        = contentGenerator;
        _analyticsAggregation    = analyticsAggregation;
        _deficiencyMetrics       = deficiencyMetrics;
        _logger                  = logger;
    }

    public async Task<AiDraftDto> GenerateAsync(
        DraftGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting draft generation. OrgId={OrgId} DraftType={DraftType} SourceType={SourceType}",
            request.OrganizationId, request.DraftType, request.SourceType);

        var template = _templateRegistry.Resolve(request.DraftType);
        var context  = await BuildContextAsync(request, cancellationToken);
        var content  = await _contentGenerator.GenerateAsync(template, context, cancellationToken);

        var groupId = Guid.NewGuid();
        var draft   = CreateDraftEntity(request, content, groupId, version: 1);

        await _draftRepository.CreateAsync(draft, cancellationToken);

        _logger.LogInformation(
            "Draft generated. Id={DraftId} GroupId={GroupId} Version=1 DraftType={DraftType}",
            draft.Id, groupId, request.DraftType);

        return MapToDto(draft);
    }

    public async Task<AiDraftDto> RegenerateAsync(
        Guid existingDraftId,
        DraftGenerationRequest? overrideRequest = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _draftRepository.GetAsync(existingDraftId, cancellationToken)
            ?? throw new InvalidOperationException($"Draft {existingDraftId} not found.");

        var allVersions = await _draftRepository.GetVersionsByGroupAsync(
            existing.GroupId, cancellationToken);

        var nextVersion = allVersions.Max(v => v.Version) + 1;

        var request = overrideRequest ?? new DraftGenerationRequest
        {
            DraftType        = existing.DraftType,
            SourceType       = existing.SourceType,
            Persona          = existing.Persona,
            OrganizationId   = existing.OrganizationId,
            RecommendationId = existing.RecommendationId,
            SourceEntityId   = existing.SourceEntityId,
            ExistingGroupId  = existing.GroupId,
        };

        _logger.LogInformation(
            "Regenerating draft. ExistingId={ExistingId} GroupId={GroupId} NewVersion={Version}",
            existingDraftId, existing.GroupId, nextVersion);

        var template = _templateRegistry.Resolve(request.DraftType);
        var context  = await BuildContextAsync(request, cancellationToken);
        var content  = await _contentGenerator.GenerateAsync(template, context, cancellationToken);

        var newDraft = CreateDraftEntity(request, content, existing.GroupId, nextVersion);
        await _draftRepository.CreateAsync(newDraft, cancellationToken);

        _logger.LogInformation(
            "Draft regenerated. NewId={NewId} GroupId={GroupId} Version={Version}",
            newDraft.Id, existing.GroupId, nextVersion);

        return MapToDto(newDraft);
    }

    public async Task<IReadOnlyList<AiDraftDto>> GetVersionsAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _draftRepository.GetAsync(draftId, cancellationToken)
            ?? throw new InvalidOperationException($"Draft {draftId} not found.");

        var versions = await _draftRepository.GetVersionsByGroupAsync(
            draft.GroupId, cancellationToken);

        return versions.Select(MapToDto).ToList();
    }

    // ── Context assembly ──────────────────────────────────────────────────────

    private async Task<DraftGenerationContext> BuildContextAsync(
        DraftGenerationRequest request,
        CancellationToken ct)
    {
        var scope = AnalyticsScope.ForHospital(request.OrganizationId, Guid.Empty, Guid.Empty);

        CopilotRecommendation? sourceRec = null;
        if (request.RecommendationId.HasValue)
        {
            sourceRec = await _recommendationRepository.GetAsync(
                request.RecommendationId.Value, ct);
        }

        // Fetch analytics sequentially — shared DbContext is not thread-safe for parallel queries
        var deficiencyCounts = await _analyticsAggregation.GetDeficiencyCountsAsync(scope, ct);
        var capaMetrics = await _analyticsAggregation.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _analyticsAggregation.GetSlaMetricsAsync(scope, ct);
        var readiness = await _analyticsAggregation.GetReadinessAsync(scope, ct);
        var governanceMetrics = await _analyticsAggregation.GetGovernanceMetricsAsync(scope, ct);

        // Build evidence summary from CAPA/deficiency context if no dedicated evidence service
        EvidenceSummaryContext? evidenceSummary = null;
        if (request.DraftType == DraftType.EvidenceChecklist ||
            request.DraftType == DraftType.ReadinessSummary)
        {
            evidenceSummary = BuildEvidenceSummaryFromContext(
                deficiencyCounts, sourceRec);
        }

        // Build stage evaluations from analytics context
        IReadOnlyList<StageEvaluationSummary>? stageEvaluations =
            BuildStageEvaluationsFromContext(readiness);

        return new DraftGenerationContext
        {
            Request              = request,
            SourceRecommendation = sourceRec,
            DeficiencyMetrics    = deficiencyCounts,
            CapaMetrics          = capaMetrics,
            SlaMetrics           = slaMetrics,
            ReadinessMetrics     = readiness,
            GovernanceMetrics    = governanceMetrics,
            StageEvaluations     = stageEvaluations,
            EvidenceSummary      = evidenceSummary,
            SnapshotUtc          = DateTime.UtcNow,
        };
    }

    private static EvidenceSummaryContext? BuildEvidenceSummaryFromContext(
        Analytics.DeficiencyMetricCountsDto? defMetrics,
        CopilotRecommendation? sourceRec)
    {
        if (defMetrics is null)
            return null;

        // Estimate evidence gaps from deficiency metrics when dedicated evidence service unavailable
        var missingEstimate = defMetrics.BlockingOpen + (defMetrics.CriticalOpen / 2);

        var missingObligations = new List<string>();
        if (sourceRec?.SourceEntityType != null)
            missingObligations.Add($"{sourceRec.SourceEntityType} — {sourceRec.Title}");

        return new EvidenceSummaryContext
        {
            TotalObligations  = Math.Max(missingEstimate * 3, 1),
            MissingEvidence   = missingEstimate,
            PendingVerification = defMetrics.BlockingOpen,
            VerifiedEvidence  = Math.Max(0, missingEstimate * 2 - defMetrics.CriticalOpen),
            TopMissingObligations = missingObligations,
        };
    }

    private static IReadOnlyList<StageEvaluationSummary>? BuildStageEvaluationsFromContext(
        Analytics.AnalyticsReadinessDto? readiness)
    {
        if (readiness is null)
            return null;

        var journey = readiness.JourneyAccreditationReadiness;
        return new List<StageEvaluationSummary>
        {
            new()
            {
                StageName            = "Accreditation Journey",
                CompletionPercent    = journey,
                IsBlocked            = journey < 60,
                BlockingIssueCount   = journey < 60 ? 1 : 0,
                MissingEvidenceCount = (int)Math.Max(0, (80 - journey) / 5),
                PendingVerificationCount = (int)Math.Max(0, (80 - journey) / 10),
            },
        };
    }

    // ── Entity creation ───────────────────────────────────────────────────────

    private static AiDraft CreateDraftEntity(
        DraftGenerationRequest request,
        DraftContent content,
        Guid groupId,
        int version)
    {
        var metadataJson = JsonSerializer.Serialize(content.SourceIntelligence,
            new JsonSerializerOptions { WriteIndented = false });

        return AiDraft.CreateFromEngine(
            organizationId:   request.OrganizationId,
            groupId:          groupId,
            version:          version,
            draftType:        request.DraftType,
            sourceType:       request.SourceType,
            persona:          request.Persona,
            title:            content.Title,
            summary:          content.Summary,
            content:          content.FullContent,
            generatedBy:      content.GeneratorName,
            confidence:       content.Confidence,
            metadataJson:     metadataJson,
            sourceEntityId:   request.SourceEntityId,
            recommendationId: request.RecommendationId);
    }

    // ── DTO mapping ───────────────────────────────────────────────────────────

    private static AiDraftDto MapToDto(AiDraft d) =>
        new()
        {
            Id               = d.Id,
            GroupId          = d.GroupId,
            OrganizationId   = d.OrganizationId,
            DraftType        = d.DraftType.ToString(),
            DraftTypeEnum    = (int)d.DraftType,
            SourceType       = d.SourceType.ToString(),
            SourceEntityId   = d.SourceEntityId,
            RecommendationId = d.RecommendationId,
            Persona          = d.Persona,
            Title            = d.Title,
            Summary          = d.Summary,
            Content          = d.Content,
            GeneratedBy      = d.GeneratedBy,
            Confidence       = d.Confidence,
            MetadataJson     = d.MetadataJson,
            Status           = d.Status.ToString(),
            Version          = d.Version,
            GeneratedAtUtc   = d.GeneratedAtUtc,
            CreatedUtc       = d.CreatedUtc,
            UpdatedUtc       = d.UpdatedUtc,
        };
}
