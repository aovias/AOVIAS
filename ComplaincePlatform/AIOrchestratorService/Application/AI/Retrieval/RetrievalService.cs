using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Knowledge;
using AIOrchestratorService.Application.AI.Models;
using Microsoft.Extensions.Options;
using System.Linq;

namespace AIOrchestratorService.Application.AI.Retrieval;

public sealed class RetrievalService : IRetrievalService
{
    private readonly IComplianceContextClient _complianceContextClient;
    private readonly IKnowledgeBaseClient _knowledgeBaseClient;
    private readonly IVectorIndex _vectorIndex;
    private readonly IKnowledgeSearchService _knowledgeSearchService;
    private readonly IOptionsMonitor<AIRetrievalPolicyOptions> _policy;

    public RetrievalService(
        IComplianceContextClient complianceContextClient,
        IKnowledgeBaseClient knowledgeBaseClient,
        IVectorIndex vectorIndex,
        IKnowledgeSearchService knowledgeSearchService,
        IOptionsMonitor<AIRetrievalPolicyOptions> policy)
    {
        _complianceContextClient = complianceContextClient;
        _knowledgeBaseClient = knowledgeBaseClient;
        _vectorIndex = vectorIndex;
        _knowledgeSearchService = knowledgeSearchService;
        _policy = policy;
    }

    public Task<KnowledgeSearchResult> SearchKnowledgeAsync(KnowledgeQuery query, CancellationToken cancellationToken)
        => _knowledgeSearchService.SearchAsync(query, cancellationToken);

    public async Task<AIRetrievalContext> BuildContextAsync(AIRequest request, CancellationToken cancellationToken)
    {
        var policy = _policy.CurrentValue;
        var bundle = await _complianceContextClient.GetContextBundleAsync(
            new ComplianceContextBundleRequest
            {
                HospitalId = request.HospitalId,
                FrameworkId = request.FrameworkId,
                OrganizationId = request.OrganizationId,
                ObjectiveElementId = request.ObjectiveElementId,
                TaskId = request.TaskId,
                EvidenceId = request.EvidenceId,
                PageContext = request.PageContext,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                ConversationSummary = request.ConversationSummary,
                RecentEntityRefs = request.RecentEntityRefs
            },
            cancellationToken);

        var kb = await _knowledgeBaseClient.AskAsync(request, cancellationToken);
        var context = new AIRetrievalContext
        {
            ContextVersionId = bundle?.ContextVersionId ?? $"{request.HospitalId:N}:{DateTime.UtcNow:yyyyMMddHHmm}",
            Department = request.Department ?? string.Empty
        };

        // Hybrid vector leg (SQL cosine + keyword); fails soft if index empty.
        if (!string.IsNullOrWhiteSpace(request.Prompt) && request.HospitalId != Guid.Empty)
        {
            try
            {
                var vectorHits = await _vectorIndex.SearchAsync(
                    request.HospitalId,
                    request.Prompt,
                    request.FrameworkId,
                    Math.Max(3, policy.MaxKBNodes),
                    cancellationToken);
                foreach (var hit in vectorHits)
                {
                    context.Sources.Add(new AISourceReference
                    {
                        SourceType = hit.SourceType,
                        SourceId = hit.SourceId,
                        DocumentId = hit.DocumentId,
                        Version = hit.Version,
                        Excerpt = hit.Excerpt,
                        Summary = string.IsNullOrWhiteSpace(hit.Title) ? hit.Excerpt : $"{hit.Title}: {hit.Excerpt}",
                        RelevanceScore = hit.Score
                    });
                }

                if (vectorHits.Count > 0)
                {
                    context.Facts["vectorHighlights"] = string.Join(
                        " · ",
                        vectorHits.Take(3).Select(h => h.Title));
                }
            }
            catch
            {
                // Keyword/KB legs below remain available.
            }
        }

        var knowledgeCandidates = kb.Take(policy.MaxKBNodes).ToList();
        if (knowledgeCandidates.Count > 0)
        {
            var highlights = knowledgeCandidates
                .Select(snippet => snippet.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Take(3)
                .ToList();
            if (highlights.Count > 0)
            {
                context.Facts["knowledgeHighlights"] = string.Join(" · ", highlights);
            }

            foreach (var snippet in knowledgeCandidates)
            {
                var summary = snippet.Text.Trim();
                if (string.IsNullOrWhiteSpace(summary))
                {
                    continue;
                }

                context.Sources.Add(new AISourceReference
                {
                    SourceType = "KnowledgeBase",
                    SourceId = snippet.Id,
                    Summary = summary,
                    Excerpt = summary.Length > 280 ? summary[..280] : summary,
                    RelevanceScore = 0.85m
                });
            }
        }

        if (bundle != null)
        {
            foreach (var fact in bundle.Facts)
            {
                if (!context.Facts.ContainsKey(fact.Key))
                {
                    context.Facts[fact.Key] = fact.Value;
                }
            }

            var boundedSources = RankAndBoundBundleSources(bundle.Sources, request, policy);
            foreach (var source in boundedSources)
            {
                context.Sources.Add(new AISourceReference
                {
                    SourceType = source.SourceType,
                    SourceId = source.SourceId,
                    Summary = source.Summary,
                    RelevanceScore = source.RelevanceScore
                });
            }
        }

        // Ranking + dedupe: same OE / department / recency / risk are pre-ranked by ComplianceService.
        context.Sources = context.Sources
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceId))
            .GroupBy(x => $"{x.SourceType}:{x.SourceId}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderByDescending(y => y.RelevanceScore).First())
            .OrderByDescending(x => x.RelevanceScore)
            .Take(policy.MaxFindings + policy.MaxEvidence + policy.MaxTasks + policy.MaxKBNodes)
            .ToList();

        context = EnforceContextBudget(context, policy);

        context.RelevanceScore = context.Sources.Count == 0
            ? 0.25m
            : Math.Clamp(context.Sources.Average(s => s.RelevanceScore), 0m, 1m);

        return context;
    }

    private static List<ComplianceContextSourceDto> RankAndBoundBundleSources(
        IReadOnlyList<ComplianceContextSourceDto> allSources,
        AIRequest request,
        AIRetrievalPolicyOptions policy)
    {
        var ranked = allSources
            .OrderByDescending(x => ScoreSource(x, request))
            .ToList();

        var oe = new List<ComplianceContextSourceDto>();
        var evidence = new List<ComplianceContextSourceDto>();
        var findings = new List<ComplianceContextSourceDto>();
        var remaining = new List<ComplianceContextSourceDto>();

        foreach (var source in ranked)
        {
            var sourceType = source.SourceType ?? string.Empty;
            if (sourceType.Contains("objective", StringComparison.OrdinalIgnoreCase) ||
                sourceType.Contains("oe", StringComparison.OrdinalIgnoreCase))
            {
                oe.Add(source);
                continue;
            }

            if (sourceType.Contains("evidence", StringComparison.OrdinalIgnoreCase))
            {
                evidence.Add(source);
                continue;
            }

            if (sourceType.Contains("finding", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(source);
                continue;
            }

            remaining.Add(source);
        }

        var result = new List<ComplianceContextSourceDto>();
        result.AddRange(oe.Take(Math.Max(1, policy.MaxObjectiveElements)));
        result.AddRange(evidence.Take(Math.Max(1, policy.MaxEvidenceItems)));
        result.AddRange(findings.Take(Math.Max(1, policy.MaxFindingsItems)));
        result.AddRange(remaining.Take(Math.Max(0, policy.MaxTasks + policy.MaxKBNodes)));
        return result;
    }

    private static decimal ScoreSource(ComplianceContextSourceDto source, AIRequest request)
    {
        var score = source.RelevanceScore;
        if (request.ObjectiveElementId.HasValue &&
            source.SourceId.Contains(request.ObjectiveElementId.Value.ToString("N"), StringComparison.OrdinalIgnoreCase))
        {
            score += 1.0m;
        }

        if (!string.IsNullOrWhiteSpace(request.Department) &&
            !string.IsNullOrWhiteSpace(source.Summary) &&
            source.Summary.Contains(request.Department, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.5m;
        }

        if (!string.IsNullOrWhiteSpace(source.Summary) &&
            source.Summary.Contains("high", StringComparison.OrdinalIgnoreCase))
        {
            score += 0.2m;
        }

        return score;
    }

    private static AIRetrievalContext EnforceContextBudget(AIRetrievalContext context, AIRetrievalPolicyOptions policy)
    {
        var maxTokens = Math.Max(200, policy.MaxContextTokens);
        var tokenBudget = EstimateTokens(context.Facts.Values) + EstimateTokens(context.Sources.Select(x => x.Summary ?? string.Empty));
        if (tokenBudget <= maxTokens)
        {
            return context;
        }

        var trimmedSources = new List<AISourceReference>();
        var runningTokens = EstimateTokens(context.Facts.Values);
        foreach (var source in context.Sources.OrderByDescending(x => x.RelevanceScore))
        {
            var sourceTokens = EstimateTokens(new[] { source.Summary ?? string.Empty });
            if (runningTokens + sourceTokens > maxTokens)
            {
                continue;
            }

            trimmedSources.Add(source);
            runningTokens += sourceTokens;
        }

        context.Sources = trimmedSources;
        return context;
    }

    private static int EstimateTokens(IEnumerable<string> values)
    {
        var totalChars = values.Sum(v => v?.Length ?? 0);
        return Math.Max(1, totalChars / 4);
    }
}
