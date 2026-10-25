using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Template-based recommendation explainer.
///
/// Generates executive-friendly, human-readable explanations from the structured
/// <see cref="RecommendationCandidate.RationaleData"/> object.
///
/// This is the Sprint 13B implementation. Sprint 13C+ may replace or augment this
/// with an LLM call (e.g. via AIOrchestratorService) while keeping the same interface.
///
/// Architecture principle:
///   Recommendation content = rule-based (deterministic).
///   Explanation = presentation layer only (replaceable with LLM).
/// </summary>
public sealed class RecommendationExplainerService : IRecommendationExplainerService
{
    public string Explain(RecommendationCandidate candidate)
    {
        var rationale = candidate.RationaleData;
        var metrics = rationale.Metrics;

        var severityLabel = candidate.Severity switch
        {
            RecommendationSeverity.Critical => "This is a critical compliance risk",
            RecommendationSeverity.High     => "This requires immediate attention",
            RecommendationSeverity.Medium   => "This should be addressed promptly",
            RecommendationSeverity.Low      => "This is a low-priority improvement opportunity",
            _                               => "Action is recommended",
        };

        var metricsSummary = BuildMetricsSummary(metrics);
        var actionText = BuildActionText(candidate);
        var impactText = candidate.RiskIfIgnored.Length > 0
            ? $" If not addressed: {candidate.RiskIfIgnored}"
            : string.Empty;

        return $"{severityLabel}. {candidate.Rationale} {metricsSummary}{actionText}{impactText}";
    }

    private static string BuildMetricsSummary(Dictionary<string, string> metrics)
    {
        if (!metrics.Any()) return string.Empty;

        var keyMetrics = metrics
            .Take(3)
            .Select(kv => $"{FormatKey(kv.Key)}: {kv.Value}")
            .ToList();

        return keyMetrics.Any()
            ? $"Key indicators — {string.Join(", ", keyMetrics)}. "
            : string.Empty;
    }

    private static string BuildActionText(RecommendationCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.ExpectedOutcome)) return string.Empty;
        return $"Expected outcome: {candidate.ExpectedOutcome}";
    }

    private static string FormatKey(string key) =>
        System.Text.RegularExpressions.Regex.Replace(
            key,
            "([a-z])([A-Z])",
            "$1 $2");
}
