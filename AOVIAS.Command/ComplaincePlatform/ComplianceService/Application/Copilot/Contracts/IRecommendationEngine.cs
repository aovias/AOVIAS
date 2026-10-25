using ComplianceService.Domain.Copilot;
using ComplianceService.Application.Copilot.Models;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Orchestrates all recommendation rules to produce a ranked, persona-filtered list
/// of recommendations for an organization.
///
/// Pipeline:
///   1. Load shared operational data (deficiency, CAPA, SLA, stage unlock, evidence metrics)
///   2. Execute all registered <see cref="IRecommendationRule"/> implementations
///   3. Rank candidates via <see cref="IRecommendationRankingService"/>
///   4. Apply persona-specific top-N filtering
///   5. Enrich with human-readable explanations via <see cref="IRecommendationExplainerService"/>
///   6. Expire stale recommendations
///   7. Persist and return results
/// </summary>
public interface IRecommendationEngine
{
    /// <summary>
    /// Run the full recommendation pipeline for an organization/hospital/framework scope.
    /// </summary>
    Task<IReadOnlyList<CopilotRecommendation>> GenerateAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        RefreshRecommendationsRequest? request = null,
        CancellationToken cancellationToken = default);
}
