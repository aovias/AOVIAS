using System.Text.Json;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Orchestrates the full Copilot recommendation pipeline.
///
/// Pipeline:
///   1. Resolve analytics scope
///   2. Pre-fetch all operational data in parallel (deficiency, CAPA, SLA, readiness, stage unlock, evidence)
///   3. Execute all registered IRecommendationRule implementations in parallel
///   4. Rank all candidates via IRecommendationRankingService
///   5. Select top-N per persona via persona filtering
///   6. Enrich with human-readable explanations (IRecommendationExplainerService)
///   7. Expire stale previous recommendations
///   8. Persist and return
///
/// CRITICAL: This service is read-only for all operational domains.
/// No writes to deficiencies, CAPAs, tasks, evidence, or any compliance state.
/// </summary>
public sealed class RecommendationEngine : IRecommendationEngine
{
    private const int TopRecommendationsPerPersona = 5;
    private const int MaxRecommendationAgeHours = 72;

    private readonly IEnumerable<IRecommendationRule> _rules;
    private readonly IRecommendationRankingService _ranker;
    private readonly IRecommendationExplainerService _explainer;
    private readonly ICopilotRecommendationRepository _repository;
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly IAnalyticsAggregationService _analytics;
    private readonly IDeficiencyMetricsService _deficiencyMetrics;
    private readonly IEscalationService _escalationService;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly IEvidenceLibraryService _evidenceLibrary;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public RecommendationEngine(
        IEnumerable<IRecommendationRule> rules,
        IRecommendationRankingService ranker,
        IRecommendationExplainerService explainer,
        ICopilotRecommendationRepository repository,
        IAnalyticsQueryService analyticsQuery,
        IAnalyticsAggregationService analytics,
        IDeficiencyMetricsService deficiencyMetrics,
        IEscalationService escalationService,
        IStageUnlockingService stageUnlocking,
        IEvidenceLibraryService evidenceLibrary)
    {
        _rules            = rules;
        _ranker           = ranker;
        _explainer        = explainer;
        _repository       = repository;
        _analyticsQuery   = analyticsQuery;
        _analytics        = analytics;
        _deficiencyMetrics = deficiencyMetrics;
        _escalationService = escalationService;
        _stageUnlocking   = stageUnlocking;
        _evidenceLibrary  = evidenceLibrary;
    }

    public async Task<IReadOnlyList<CopilotRecommendation>> GenerateAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        RefreshRecommendationsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve scope
        var resolvedScope = await _analyticsQuery.ResolveScopeAsync(
            organizationId, hospitalId, frameworkId ?? Guid.Empty, cancellationToken);

        var scope = resolvedScope ?? AnalyticsScope.ForHospital(organizationId, hospitalId, frameworkId ?? Guid.Empty);

        // 2. Pre-fetch all operational context in parallel
        var context = await BuildContextAsync(scope, request, cancellationToken);

        // 3. Execute rules sequentially — DbContext is scoped per request and not thread-safe
        var allCandidates = new List<RecommendationCandidate>();
        foreach (var rule in FilterRules(request?.Persona))
        {
            var results = await rule.EvaluateAsync(context, cancellationToken);
            allCandidates.AddRange(results);
        }

        if (!allCandidates.Any())
        {
            // Expire stale recommendations even when no new ones generated
            await ExpireStaleAsync(organizationId, cancellationToken);
            return Array.Empty<CopilotRecommendation>();
        }

        // 4. Enrich with human-readable explanations (LLM layer — template-based in Sprint 13B)
        foreach (var candidate in allCandidates)
            candidate.Description = _explainer.Explain(candidate);

        // 5. Rank all candidates
        var ranked = _ranker.Rank(allCandidates);

        // 6. Select top-N per persona
        var topCandidates = _ranker.FilterTopPerPersona(ranked, TopRecommendationsPerPersona);

        // 7. Persist as CopilotRecommendation entities (with Sprint 13C impact estimates)
        var recommendations = topCandidates
            .Select(c => ToEntityWithImpact(organizationId, c, context))
            .ToList();

        await _repository.BulkCreateAsync(recommendations, cancellationToken);

        // 8. Expire stale previous recommendations
        await ExpireStaleAsync(organizationId, cancellationToken);

        return recommendations;
    }

    // ── Context builder ─────────────────────────────────────────────────────

    private async Task<RecommendationRuleContext> BuildContextAsync(
        AnalyticsScope scope,
        RefreshRecommendationsRequest? request,
        CancellationToken ct)
    {
        // Sequential fetch — shared scoped DbContext is not safe for parallel queries
        var deficiencyMetrics = await _deficiencyMetrics.GetCountsAsync(scope, ct);
        var capaMetrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var slaMetrics = await _escalationService.GetMetricsForScopeAsync(scope.OrganizationId, scope.HospitalId, scope.FrameworkId, ct);
        var readinessMetrics = await _analytics.GetReadinessAsync(scope, ct);
        var governanceMetrics = await _analytics.GetGovernanceMetricsAsync(scope, ct);
        var stageEvaluations = await _stageUnlocking.GetStageEvaluationsAsync(scope.HospitalId, scope.OrganizationId, scope.FrameworkId, ct);
        var evidenceSummary = await _evidenceLibrary.GetSummaryAsync(scope.HospitalId, scope.FrameworkId, ct);

        return new RecommendationRuleContext
        {
            Scope             = scope,
            CopilotContext    = request?.Context is not null ? MapContext(scope.OrganizationId, request.Context) : null,
            DeficiencyMetrics = deficiencyMetrics,
            CapaMetrics       = capaMetrics,
            SlaMetrics        = slaMetrics,
            ReadinessMetrics  = readinessMetrics,
            GovernanceMetrics = governanceMetrics,
            StageEvaluations  = stageEvaluations,
            EvidenceSummary   = evidenceSummary,
        };
    }

    private IEnumerable<IRecommendationRule> FilterRules(string? personaFilter)
    {
        if (string.IsNullOrWhiteSpace(personaFilter)) return _rules;

        if (Enum.TryParse<CopilotPersona>(personaFilter, out var persona))
            return _rules.Where(r => r.TargetPersonas.Contains(persona));

        return _rules;
    }

    // ── Entity builder ───────────────────────────────────────────────────────

    private static CopilotRecommendation ToEntityWithImpact(
        Guid organizationId,
        RecommendationCandidate c,
        RecommendationRuleContext context)
    {
        string? rationaleJson = null;
        try { rationaleJson = JsonSerializer.Serialize(c.RationaleData, JsonOptions); }
        catch { /* serialization failure does not block persistence */ }

        // Sprint 13C: estimate impact and attach to entity
        string? impactJson = null;
        try
        {
            var impact = RecommendationImpactEstimator.Estimate(c, context);
            impactJson = RecommendationImpactEstimator.Serialize(impact);
        }
        catch { /* impact estimation failure does not block persistence */ }

        var entity = CopilotRecommendation.CreateFromRuleEngine(
            organizationId:     organizationId,
            recommendationType: c.RecommendationType,
            category:           c.Category,
            severity:           c.Severity,
            persona:            c.Persona,
            priority:           c.Priority,
            confidence:         c.Confidence,
            title:              c.Title,
            description:        c.Description,
            rationale:          c.Rationale,
            expectedOutcome:    c.ExpectedOutcome,
            riskIfIgnored:      c.RiskIfIgnored,
            sourceReferences:   c.SourceReferences,
            sourceEntityType:   c.SourceEntityType,
            sourceEntityId:     c.SourceEntityId,
            deepLink:           c.DeepLink,
            rationaleJson:      rationaleJson,
            expiresAtUtc:       c.ExpiresAtUtc);

        if (impactJson is not null)
            entity.SetImpact(impactJson);

        return entity;
    }

    // Legacy overload kept for backward compatibility
    private static CopilotRecommendation ToEntity(Guid organizationId, RecommendationCandidate c) =>
        ToEntityWithImpact(organizationId, c, new RecommendationRuleContext { Scope = AnalyticsScope.ForHospital(organizationId, Guid.Empty, Guid.Empty) });

    private static CopilotContext? MapContext(Guid organizationId, DTOs.CopilotContextDto dto) =>
        dto is null ? null : new CopilotContext
        {
            OrganizationId   = organizationId,
            PageContext       = dto.PageContext ?? string.Empty,
            FocusedEntityId   = dto.FocusedEntityId,
            FocusedEntityType = dto.FocusedEntityType,
        };

    private async Task ExpireStaleAsync(Guid organizationId, CancellationToken ct)
    {
        var olderThan = DateTime.UtcNow.AddHours(-MaxRecommendationAgeHours);
        await _repository.ExpireStaleAsync(organizationId, olderThan, ct);
    }
}
