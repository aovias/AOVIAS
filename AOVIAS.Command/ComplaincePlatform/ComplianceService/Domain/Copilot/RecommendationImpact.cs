namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Estimated impact of accepting a Copilot recommendation.
/// Stored as JSON on <see cref="CopilotRecommendation"/> and surfaced in generated drafts.
/// All values are estimates — not guarantees.
/// </summary>
public sealed record RecommendationImpact
{
    /// <summary>Estimated improvement to the accreditation readiness score (0–100 scale).</summary>
    public decimal EstimatedReadinessImpact { get; init; }

    /// <summary>Estimated improvement to the Case Mix Index (0.00–2.00+ scale).</summary>
    public decimal EstimatedCmiImpact { get; init; }

    /// <summary>Estimated number of deficiencies that would be reduced.</summary>
    public int EstimatedDeficienciesReduced { get; init; }

    /// <summary>Estimated number of SLA breaches that would be prevented.</summary>
    public int EstimatedSlaBreachesPrevented { get; init; }

    /// <summary>Estimated number of CAPA items that would be improved or closed.</summary>
    public int EstimatedCapaImprovements { get; init; }

    /// <summary>Returns a zero-impact record (no estimation available).</summary>
    public static RecommendationImpact Zero => new();

    /// <summary>Returns true if any impact estimate is non-zero.</summary>
    public bool HasImpact =>
        EstimatedReadinessImpact > 0 ||
        EstimatedCmiImpact > 0 ||
        EstimatedDeficienciesReduced > 0 ||
        EstimatedSlaBreachesPrevented > 0 ||
        EstimatedCapaImprovements > 0;
}
