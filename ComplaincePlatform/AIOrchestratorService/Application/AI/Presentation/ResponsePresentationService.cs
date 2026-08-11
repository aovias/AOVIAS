using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Validation;

namespace AIOrchestratorService.Application.AI.Presentation;

/// <summary>
/// Transforms the raw orchestration output into the executive Compliance Advisor presentation
/// model the UI renders as cards. All user-facing strings are scrubbed of AI/retrieval
/// terminology, and technical details (confidence, sources) are tucked into <see cref="TechnicalMetadata"/>.
/// </summary>
public interface IResponsePresentationService
{
    ComplianceResponseModel Build(
        AIRequest request,
        AIResponse response,
        AIModelResponse modelResponse,
        AssistantAnswerMode answerMode);
}

public sealed class ResponsePresentationService : IResponsePresentationService
{
    private readonly IResponseSanitizer _sanitizer;

    public ResponsePresentationService(IResponseSanitizer sanitizer)
    {
        _sanitizer = sanitizer;
    }

    public ComplianceResponseModel Build(
        AIRequest request,
        AIResponse response,
        AIModelResponse modelResponse,
        AssistantAnswerMode answerMode)
    {
        var executiveSummary = ResolveExecutiveSummary(response, modelResponse, answerMode);

        var model = new ComplianceResponseModel
        {
            InsightType = ResolveInsightType(request, modelResponse),
            ExecutiveSummary = executiveSummary,
            Findings = BuildFindings(modelResponse),
            Risks = BuildRisks(modelResponse),
            Recommendations = BuildRecommendations(modelResponse),
            ExpectedOutcome = _sanitizer.ScrubInline(modelResponse.ExpectedOutcome),
            SupportingEvidence = BuildSupportingEvidence(response.SourceReferences),
            Technical = new TechnicalMetadata
            {
                Confidence = ToPercent(response.CompositeConfidenceScore),
                AnswerMode = answerMode.ToString(),
                ContextVersionId = response.ContextVersionId,
                RawSources = response.SourceReferences
            }
        };

        return model;
    }

    private string ResolveExecutiveSummary(
        AIResponse response,
        AIModelResponse modelResponse,
        AssistantAnswerMode answerMode)
    {
        var fromModel = _sanitizer.ScrubInline(modelResponse.ExecutiveSummary);
        if (!string.IsNullOrWhiteSpace(fromModel))
        {
            return fromModel;
        }

        // ComplianceHint has already been sanitized (incl. fallback) by the orchestrator.
        var hint = (response.ComplianceHint ?? string.Empty).Trim();
        return hint.Length > 0 ? hint : _sanitizer.Sanitize(string.Empty, answerMode);
    }

    private List<ComplianceFinding> BuildFindings(AIModelResponse modelResponse)
    {
        var findings = new List<ComplianceFinding>();
        foreach (var finding in modelResponse.Findings)
        {
            var title = _sanitizer.ScrubInline(finding.Title);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            findings.Add(new ComplianceFinding
            {
                Title = title,
                Severity = finding.Severity,
                Impact = _sanitizer.ScrubInline(finding.Impact),
                Detail = _sanitizer.ScrubInline(finding.Detail)
            });
        }

        return findings;
    }

    private List<ComplianceRisk> BuildRisks(AIModelResponse modelResponse)
    {
        var risks = new List<ComplianceRisk>();
        foreach (var risk in modelResponse.Risks)
        {
            var title = _sanitizer.ScrubInline(risk.Title);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            risks.Add(new ComplianceRisk
            {
                Title = title,
                Severity = risk.Severity,
                AffectedControls = risk.AffectedControls
                    .Select(c => _sanitizer.ScrubInline(c))
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList()
            });
        }

        return risks;
    }

    private List<RecommendedAction> BuildRecommendations(AIModelResponse modelResponse)
    {
        var ordered = modelResponse.Recommendations
            .Select(r => new RecommendedAction
            {
                Priority = r.Priority,
                Action = _sanitizer.ScrubInline(r.Action),
                ExpectedOutcome = _sanitizer.ScrubInline(r.ExpectedOutcome)
            })
            .Where(r => !string.IsNullOrWhiteSpace(r.Action))
            .OrderBy(r => r.Priority <= 0 ? int.MaxValue : r.Priority)
            .ToList();

        // Re-number sequentially so the UI always shows clean Priority 1..n.
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Priority = i + 1;
        }

        return ordered;
    }

    private List<SupportingEvidenceItem> BuildSupportingEvidence(IEnumerable<AISourceReference> sources)
    {
        var items = new List<SupportingEvidenceItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var type = (source.SourceType ?? string.Empty).Trim();
            var lower = type.ToLowerInvariant();

            // Hide internal/system markers from users.
            if (lower is "system" or "fallback" or "deterministicfallback")
            {
                continue;
            }

            string friendlyType;
            string label;

            if (lower.Contains("model") || lower.Contains("knowledge"))
            {
                friendlyType = "Knowledge base";
                label = "General compliance knowledge";
            }
            else
            {
                friendlyType = FriendlyType(lower);
                // Never surface raw UUID source ids; prefer a human summary, else the friendly type.
                var summary = _sanitizer.ScrubInline(source.Summary);
                label = string.IsNullOrWhiteSpace(summary) ? friendlyType : Truncate(summary, 120);
            }

            var key = $"{friendlyType}|{label}";
            if (seen.Add(key))
            {
                items.Add(new SupportingEvidenceItem { Label = label, SourceType = friendlyType });
            }
        }

        return items;
    }

    private static string FriendlyType(string lowerType) => lowerType switch
    {
        "finding" => "Audit finding",
        "task" => "Compliance task",
        "evidence" => "Evidence document",
        "objectiveelement" or "oe" => "Objective element",
        "knowledgebase" or "knowledge" => "Knowledge base",
        "audit" => "Audit record",
        _ => "Compliance record"
    };

    private static ComplianceInsightType ResolveInsightType(AIRequest request, AIModelResponse modelResponse)
    {
        if (modelResponse.InsightType.HasValue)
        {
            return modelResponse.InsightType.Value;
        }

        var prompt = (request.Prompt ?? string.Empty).ToLowerInvariant();
        if (prompt.Contains("risk"))
        {
            return ComplianceInsightType.Risk;
        }

        if (prompt.Contains("evidence") || prompt.Contains("document") || prompt.Contains("upload"))
        {
            return ComplianceInsightType.Evidence;
        }

        if (prompt.Contains("score") || prompt.Contains("readiness") || prompt.Contains("compliance level"))
        {
            return ComplianceInsightType.ComplianceScore;
        }

        if (prompt.Contains("standard") || prompt.Contains("clause") || prompt.Contains("objective element"))
        {
            return ComplianceInsightType.Standard;
        }

        return ComplianceInsightType.General;
    }

    private static int ToPercent(decimal score)
    {
        var normalized = score <= 1m ? score * 100m : score;
        var rounded = (int)Math.Round(normalized, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 100);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].TrimEnd() + "…";
    }
}
