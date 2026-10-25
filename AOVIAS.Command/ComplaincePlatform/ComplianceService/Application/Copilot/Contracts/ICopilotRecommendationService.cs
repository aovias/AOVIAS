using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Application service contract for Copilot recommendations.
/// Sprint 13B: adds rule-engine generation via RefreshAsync.
/// </summary>
public interface ICopilotRecommendationService
{
    /// <summary>Returns existing recommendations for an organization (paginated).</summary>
    Task<IReadOnlyList<CopilotRecommendationDto>> GetRecommendationsAsync(
        Guid organizationId,
        CopilotContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns top-priority recommendations for a specific persona.</summary>
    Task<IReadOnlyList<CopilotRecommendationDto>> GetByPersonaAsync(
        Guid organizationId,
        string persona,
        int maxCount = 5,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single recommendation by ID.</summary>
    Task<CopilotRecommendationDto?> GetRecommendationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprint 13B: Triggers the rule engine to regenerate recommendations.
    /// Reads operational data, runs all deterministic rules, ranks and persists results.
    /// No writes to operational domains.
    /// </summary>
    Task<IReadOnlyList<CopilotRecommendationDto>> RefreshAsync(
        Guid organizationId,
        Guid hospitalId,
        RefreshRecommendationsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Accepts a recommendation. Sprint 13D: may trigger approval card workflow.</summary>
    Task AcceptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Dismisses a recommendation.</summary>
    Task DismissAsync(Guid id, CancellationToken cancellationToken = default);
}
