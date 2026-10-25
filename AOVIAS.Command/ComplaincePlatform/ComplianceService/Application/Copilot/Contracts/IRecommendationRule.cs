using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// A single deterministic compliance recommendation rule.
/// Each rule reads operational data from <see cref="RecommendationRuleContext"/>
/// and emits zero or more <see cref="RecommendationCandidate"/> objects.
/// Rules must never write to any operational domain.
/// </summary>
public interface IRecommendationRule
{
    /// <summary>Stable identifier used in RationaleJson, audit logs, and effectiveness scoring.</summary>
    string RuleName { get; }

    /// <summary>Compliance domain this rule evaluates.</summary>
    RecommendationCategory Category { get; }

    /// <summary>Personas for which this rule's output is relevant.</summary>
    IReadOnlyList<CopilotPersona> TargetPersonas { get; }

    /// <summary>
    /// Evaluate the rule against pre-fetched operational data in <paramref name="context"/>.
    /// Returns an empty list when no recommendation is warranted.
    /// Must be pure read — no side effects.
    /// </summary>
    Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default);
}
