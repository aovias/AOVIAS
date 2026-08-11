using ComplianceService.Application.Copilot.Models;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Ranks recommendation candidates using a multi-factor scoring model.
/// Assigns <see cref="RecommendationCandidate.RankScore"/> and sets <see cref="RecommendationCandidate.Priority"/>.
/// </summary>
public interface IRecommendationRankingService
{
    /// <summary>
    /// Score and rank all candidates. Mutates <see cref="RecommendationCandidate.RankScore"/>
    /// and <see cref="RecommendationCandidate.Priority"/> in place.
    /// Returns sorted list (highest priority first).
    /// </summary>
    IReadOnlyList<RecommendationCandidate> Rank(IReadOnlyList<RecommendationCandidate> candidates);

    /// <summary>
    /// Return the top <paramref name="maxPerPersona"/> candidates for each distinct persona.
    /// </summary>
    IReadOnlyList<RecommendationCandidate> FilterTopPerPersona(
        IReadOnlyList<RecommendationCandidate> ranked,
        int maxPerPersona = 5);
}
