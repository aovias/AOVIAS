using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Multi-factor ranking of recommendation candidates.
///
/// Score factors:
///   Severity       (40%) — Critical:100, High:75, Medium:50, Low:25
///   Category urgency (25%) — Accreditation/Risk blocking categories score higher
///   Confidence      (20%) — rule confidence weight (0–1)
///   Recurrence boost(15%) — CAPA/Deficiency recurrence categories get a boost
///
/// Output: Priority 1 = highest urgency (lowest integer priority = surfaced first).
/// </summary>
public sealed class RecommendationRankingService : IRecommendationRankingService
{
    private static readonly Dictionary<RecommendationSeverity, decimal> SeverityScores = new()
    {
        [RecommendationSeverity.Critical] = 100m,
        [RecommendationSeverity.High]     = 75m,
        [RecommendationSeverity.Medium]   = 50m,
        [RecommendationSeverity.Low]      = 25m,
    };

    private static readonly Dictionary<RecommendationCategory, decimal> CategoryUrgency = new()
    {
        [RecommendationCategory.Accreditation] = 100m,
        [RecommendationCategory.Risk]          = 95m,
        [RecommendationCategory.Escalation]    = 90m,
        [RecommendationCategory.SLA]           = 85m,
        [RecommendationCategory.Deficiency]    = 80m,
        [RecommendationCategory.CAPA]          = 75m,
        [RecommendationCategory.Evidence]      = 70m,
        [RecommendationCategory.Verification]  = 65m,
        [RecommendationCategory.Executive]     = 60m,
        [RecommendationCategory.Department]    = 55m,
        [RecommendationCategory.Analytics]     = 40m,
        [RecommendationCategory.Task]          = 35m,
    };

    private static readonly HashSet<RecommendationCategory> RecurrenceBoostCategories =
        [RecommendationCategory.CAPA, RecommendationCategory.Deficiency];

    public IReadOnlyList<RecommendationCandidate> Rank(IReadOnlyList<RecommendationCandidate> candidates)
    {
        if (!candidates.Any()) return candidates;

        var ranked = candidates.ToList();
        foreach (var c in ranked)
        {
            var severityScore   = SeverityScores.TryGetValue(c.Severity, out var sv) ? sv : 50m;
            var categoryScore   = CategoryUrgency.TryGetValue(c.Category, out var cs) ? cs : 50m;
            var confidenceScore = Math.Clamp(c.Confidence, 0m, 1m) * 100m;
            var recurrenceBoost = RecurrenceBoostCategories.Contains(c.Category) ? 10m : 0m;

            c.RankScore = (severityScore * 0.40m)
                        + (categoryScore * 0.25m)
                        + (confidenceScore * 0.20m)
                        + (recurrenceBoost * 0.15m);
        }

        var sorted = ranked.OrderByDescending(c => c.RankScore).ToList();

        // Assign priority: 1 = highest urgency
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].Priority = i + 1;

        return sorted;
    }

    public IReadOnlyList<RecommendationCandidate> FilterTopPerPersona(
        IReadOnlyList<RecommendationCandidate> ranked,
        int maxPerPersona = 5)
    {
        return ranked
            .GroupBy(c => c.Persona)
            .SelectMany(g => g.Take(maxPerPersona))
            .OrderBy(c => c.Priority)
            .ToList();
    }
}
