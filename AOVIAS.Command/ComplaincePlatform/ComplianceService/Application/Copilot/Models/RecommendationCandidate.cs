using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Intermediate value object produced by a rule evaluation before ranking and persistence.
/// Contains all fields required to construct a <see cref="CopilotRecommendation"/>.
/// </summary>
public sealed class RecommendationCandidate
{
    public required string RecommendationType { get; init; }
    public required RecommendationCategory Category { get; init; }
    public required RecommendationSeverity Severity { get; init; }
    public required string Persona { get; init; }
    public int Priority { get; set; }
    public required string Title { get; init; }
    public string Description { get; set; } = string.Empty;
    public required string Rationale { get; init; }
    public string ExpectedOutcome { get; init; } = string.Empty;
    public string RiskIfIgnored { get; init; } = string.Empty;
    public string? SourceReferences { get; init; }
    public string? SourceEntityType { get; init; }
    public Guid? SourceEntityId { get; init; }
    public string? DeepLink { get; init; }
    public decimal Confidence { get; init; } = 1.0m;
    public DateTime? ExpiresAtUtc { get; init; }

    /// <summary>Machine-readable rationale object serialized to JSON on persist.</summary>
    public RecommendationRationale RationaleData { get; init; } = new();

    /// <summary>Computed ranking score set by <see cref="IRecommendationRankingService"/>.</summary>
    public decimal RankScore { get; set; }
}
