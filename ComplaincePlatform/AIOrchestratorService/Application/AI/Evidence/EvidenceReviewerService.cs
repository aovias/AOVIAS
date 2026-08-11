using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;

namespace AIOrchestratorService.Application.AI.Evidence;

public interface IEvidenceReviewerService
{
    Task<EvidenceReviewResult> ReviewAsync(EvidenceReviewRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Advisory evidence reviewer. Scores completeness/freshness/duplicate/version/OE coverage
/// and always requires human approval — never auto-approves evidence.
/// </summary>
public sealed class EvidenceReviewerService : IEvidenceReviewerService
{
    private readonly IComplianceContextClient _contextClient;
    private readonly IRetrievalService _retrievalService;
    private readonly IAIAuditService _auditService;
    private readonly ILogger<EvidenceReviewerService> _logger;

    public EvidenceReviewerService(
        IComplianceContextClient contextClient,
        IRetrievalService retrievalService,
        IAIAuditService auditService,
        ILogger<EvidenceReviewerService> logger)
    {
        _contextClient = contextClient;
        _retrievalService = retrievalService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<EvidenceReviewResult> ReviewAsync(EvidenceReviewRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = Guid.NewGuid().ToString("N");
        var bundle = await _contextClient.GetContextBundleAsync(new ComplianceContextBundleRequest
        {
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            OrganizationId = request.OrganizationId,
            ObjectiveElementId = request.ObjectiveElementId,
            EvidenceId = request.EvidenceId,
            EntityType = "Evidence",
            EntityId = request.EvidenceId?.ToString()
        }, cancellationToken);

        var facts = bundle?.Facts ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = (bundle?.Sources ?? new List<ComplianceContextSourceDto>())
            .Select(s => new AISourceReference
            {
                SourceType = s.SourceType,
                SourceId = s.SourceId,
                Summary = s.Summary,
                RelevanceScore = s.RelevanceScore
            }).ToList();

        // Optional hybrid knowledge enrichment for OE wording / policy excerpts.
        if (request.ObjectiveElementId.HasValue || !string.IsNullOrWhiteSpace(request.EvidenceTitle))
        {
            try
            {
                var search = await _retrievalService.SearchKnowledgeAsync(new KnowledgeQuery
                {
                    Query = request.EvidenceTitle ?? request.EvidenceContentSummary ?? "evidence requirements",
                    HospitalId = request.HospitalId,
                    FrameworkId = request.FrameworkId,
                    Mode = "hybrid",
                    TopK = 5
                }, cancellationToken);
                foreach (var citation in search.Citations.Take(3))
                {
                    sources.Add(citation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Knowledge enrichment skipped during evidence review");
            }
        }

        var completeness = ScoreCompleteness(request, facts);
        var freshness = ScoreFreshness(request, facts);
        var duplicateRisk = ScoreDuplicateRisk(request, facts);
        var version = ScoreVersionConsistency(request, facts);
        var oeCoverage = ScoreOeCoverage(request, facts);
        var overall = Math.Round(
            (completeness * 0.30m) +
            (freshness * 0.20m) +
            ((1m - duplicateRisk) * 0.15m) +
            (version * 0.15m) +
            (oeCoverage * 0.20m), 2);

        var recommendation = BuildRecommendation(overall, completeness, freshness, duplicateRisk, version, oeCoverage);
        var result = new EvidenceReviewResult
        {
            Scores = new EvidenceReviewScoreCard
            {
                CompletenessScore = completeness,
                FreshnessScore = freshness,
                DuplicateRiskScore = duplicateRisk,
                VersionConsistencyScore = version,
                OeCoverageScore = oeCoverage,
                OverallScore = overall
            },
            Recommendation = recommendation,
            Reasoning =
            [
                new AIReasoningStep { Code = "COMPLETENESS", Message = $"Completeness scored {completeness:P0}." },
                new AIReasoningStep { Code = "FRESHNESS", Message = $"Freshness scored {freshness:P0}." },
                new AIReasoningStep { Code = "DUPLICATE", Message = $"Duplicate risk scored {duplicateRisk:P0} (higher is riskier)." },
                new AIReasoningStep { Code = "VERSION", Message = $"Version consistency scored {version:P0}." },
                new AIReasoningStep { Code = "OE_COVERAGE", Message = $"OE coverage scored {oeCoverage:P0}." },
                new AIReasoningStep { Code = "HUMAN_GATE", Message = "Human approval is required. AI never auto-approves evidence." }
            ],
            Citations = sources,
            HumanApprovalRequired = true,
            TraceId = traceId
        };

        await _auditService.LogAsync(
            new AIRequest
            {
                HospitalId = request.HospitalId,
                FrameworkId = request.FrameworkId,
                OrganizationId = request.OrganizationId,
                ObjectiveElementId = request.ObjectiveElementId,
                EvidenceId = request.EvidenceId,
                Prompt = $"Evidence review: {request.EvidenceTitle ?? request.EvidenceId?.ToString() ?? "n/a"}",
                PromptTemplateId = "ValidateEvidence",
                PromptTemplateVersion = "1.0",
                UseCase = AIUseCase.ValidateEvidence,
                ContextVersionId = bundle?.ContextVersionId ?? traceId
            },
            new AIResponse
            {
                ComplianceHint = recommendation,
                ConfidenceScore = overall,
                CompositeConfidenceScore = overall,
                HumanApprovalRequired = true,
                ValidationOutcome = AIValidationOutcome.Accept,
                SourceReferences = sources,
                Reasoning = result.Reasoning,
                PromptTemplateId = "ValidateEvidence",
                PromptTemplateVersion = "1.0",
                ContextVersionId = bundle?.ContextVersionId ?? traceId,
                TraceId = traceId
            },
            "evidence.review",
            cancellationToken);

        return result;
    }

    private static decimal ScoreCompleteness(EvidenceReviewRequest request, IReadOnlyDictionary<string, string> facts)
    {
        var score = 0.35m;
        if (!string.IsNullOrWhiteSpace(request.EvidenceTitle)) score += 0.15m;
        if (!string.IsNullOrWhiteSpace(request.EvidenceContentSummary) && request.EvidenceContentSummary.Length >= 40) score += 0.20m;
        if (request.EvidenceId.HasValue) score += 0.10m;
        if (request.ObjectiveElementId.HasValue) score += 0.10m;
        if (HasFact(facts, "evidenceLinked", "true") || HasFact(facts, "hasEvidence", "true")) score += 0.10m;
        return Math.Clamp(score, 0m, 1m);
    }

    private static decimal ScoreFreshness(EvidenceReviewRequest request, IReadOnlyDictionary<string, string> facts)
    {
        DateTime? modified = request.EvidenceLastModifiedUtc;
        if (!modified.HasValue &&
            facts.TryGetValue("evidenceLastModifiedUtc", out var raw) &&
            DateTime.TryParse(raw, out var parsed))
        {
            modified = parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        if (!modified.HasValue)
        {
            return 0.45m;
        }

        var ageDays = (DateTime.UtcNow - modified.Value.ToUniversalTime()).TotalDays;
        if (ageDays <= 90) return 0.95m;
        if (ageDays <= 180) return 0.80m;
        if (ageDays <= 365) return 0.60m;
        if (ageDays <= 730) return 0.40m;
        return 0.25m;
    }

    private static decimal ScoreDuplicateRisk(EvidenceReviewRequest request, IReadOnlyDictionary<string, string> facts)
    {
        if (request.LinkedEvidenceIds.Count > 1) return 0.55m;
        if (facts.TryGetValue("duplicateEvidenceCount", out var raw) &&
            int.TryParse(raw, out var count) &&
            count > 0)
        {
            return Math.Clamp(0.35m + (count * 0.15m), 0m, 0.95m);
        }

        if (HasFact(facts, "possibleDuplicate", "true")) return 0.70m;
        return 0.15m;
    }

    private static decimal ScoreVersionConsistency(EvidenceReviewRequest request, IReadOnlyDictionary<string, string> facts)
    {
        if (!string.IsNullOrWhiteSpace(request.DocumentVersion))
        {
            if (facts.TryGetValue("expectedDocumentVersion", out var expected) &&
                !string.Equals(expected, request.DocumentVersion, StringComparison.OrdinalIgnoreCase))
            {
                return 0.35m;
            }

            return 0.90m;
        }

        return facts.ContainsKey("documentVersion") ? 0.70m : 0.50m;
    }

    private static decimal ScoreOeCoverage(EvidenceReviewRequest request, IReadOnlyDictionary<string, string> facts)
    {
        if (!request.ObjectiveElementId.HasValue) return 0.40m;
        if (HasFact(facts, "oeEvidenceComplete", "true")) return 0.95m;
        if (facts.TryGetValue("oeEvidenceCoverage", out var pctRaw) &&
            decimal.TryParse(pctRaw.TrimEnd('%'), out var pct))
        {
            return Math.Clamp(pct > 1m ? pct / 100m : pct, 0m, 1m);
        }

        if (HasFact(facts, "hasEvidence", "true") || HasFact(facts, "evidenceLinked", "true")) return 0.70m;
        return 0.35m;
    }

    private static string BuildRecommendation(
        decimal overall,
        decimal completeness,
        decimal freshness,
        decimal duplicateRisk,
        decimal version,
        decimal oeCoverage)
    {
        if (overall >= 0.80m && duplicateRisk < 0.40m)
        {
            return "Evidence appears sufficiently complete for reviewer consideration. Recommend human quality review before approval — AI will not auto-approve.";
        }

        var gaps = new List<string>();
        if (completeness < 0.60m) gaps.Add("add missing descriptive detail / attachments");
        if (freshness < 0.60m) gaps.Add("refresh outdated documents");
        if (duplicateRisk >= 0.50m) gaps.Add("resolve possible duplicates");
        if (version < 0.60m) gaps.Add("align document version with controlled copy");
        if (oeCoverage < 0.60m) gaps.Add("strengthen OE requirement coverage");

        var gapText = gaps.Count > 0 ? string.Join("; ", gaps) : "perform a targeted quality check";
        return $"Evidence needs improvement before approval: {gapText}. Human approval remains mandatory.";
    }

    private static bool HasFact(IReadOnlyDictionary<string, string> facts, string key, string expected) =>
        facts.TryGetValue(key, out var value) &&
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
