using ComplianceService.Application.Analytics;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules;

/// <summary>
/// Base class for all recommendation rules. Provides helpers for building
/// <see cref="RecommendationCandidate"/> objects consistently.
/// </summary>
public abstract class RecommendationRuleBase : IRecommendationRule
{
    public abstract string RuleName { get; }
    public abstract RecommendationCategory Category { get; }
    public abstract IReadOnlyList<CopilotPersona> TargetPersonas { get; }

    public abstract Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Build a candidate for every target persona of this rule.</summary>
    protected IReadOnlyList<RecommendationCandidate> BuildForAllPersonas(
        RecommendationSeverity severity,
        string title,
        string rationale,
        string expectedOutcome,
        string riskIfIgnored,
        RecommendationRationale rationaleData,
        decimal confidence = 1.0m,
        string? sourceEntityType = null,
        Guid? sourceEntityId = null,
        string? deepLink = null,
        DateTime? expiresAtUtc = null)
    {
        return TargetPersonas
            .Select(p => Build(
                persona: p.ToString(),
                severity: severity,
                title: title,
                rationale: rationale,
                expectedOutcome: expectedOutcome,
                riskIfIgnored: riskIfIgnored,
                rationaleData: rationaleData,
                confidence: confidence,
                sourceEntityType: sourceEntityType,
                sourceEntityId: sourceEntityId,
                deepLink: deepLink,
                expiresAtUtc: expiresAtUtc))
            .ToList();
    }

    /// <summary>Build a single candidate for a named persona string.</summary>
    protected RecommendationCandidate Build(
        string persona,
        RecommendationSeverity severity,
        string title,
        string rationale,
        string expectedOutcome,
        string riskIfIgnored,
        RecommendationRationale rationaleData,
        decimal confidence = 1.0m,
        string? sourceEntityType = null,
        Guid? sourceEntityId = null,
        string? deepLink = null,
        DateTime? expiresAtUtc = null) =>
        new()
        {
            RecommendationType = RuleName,
            Category           = Category,
            Severity           = severity,
            Persona            = persona,
            Title              = title,
            Description        = string.Empty,  // populated by IRecommendationExplainerService
            Rationale          = rationale,
            ExpectedOutcome    = expectedOutcome,
            RiskIfIgnored      = riskIfIgnored,
            RationaleData      = rationaleData,
            Confidence         = confidence,
            SourceEntityType   = sourceEntityType,
            SourceEntityId     = sourceEntityId,
            DeepLink           = deepLink,
            ExpiresAtUtc       = expiresAtUtc,
        };

    protected static RecommendationRationale MakeRationale(
        string ruleName,
        Dictionary<string, string> metrics,
        Dictionary<string, string>? thresholds = null,
        List<RationaleSourceEntity>? sources = null,
        AnalyticsScope? scope = null) =>
        new()
        {
            RuleName       = ruleName,
            Metrics        = metrics,
            Thresholds     = thresholds ?? new(),
            SourceEntities = sources ?? new(),
            DataAsOfUtc    = DateTime.UtcNow.ToString("O"),
            ScopeRef       = scope is null ? string.Empty
                             : $"{scope.OrganizationId}/{scope.HospitalId}/{scope.FrameworkId}",
        };
}
