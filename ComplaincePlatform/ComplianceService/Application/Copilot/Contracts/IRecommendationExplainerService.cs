using ComplianceService.Application.Copilot.Models;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Converts a <see cref="RecommendationCandidate"/> into a human-readable, executive-friendly
/// explanation string that populates <see cref="RecommendationCandidate.Description"/>.
///
/// The initial implementation (<see cref="Services.RecommendationExplainerService"/>) uses
/// deterministic templates. Sprint 13C+ can replace or supplement this with an LLM call.
///
/// The recommendation itself is always rule-based; the explainer is a presentation layer only.
/// </summary>
public interface IRecommendationExplainerService
{
    /// <summary>Generate an executive-friendly explanation for a single candidate.</summary>
    string Explain(RecommendationCandidate candidate);
}
