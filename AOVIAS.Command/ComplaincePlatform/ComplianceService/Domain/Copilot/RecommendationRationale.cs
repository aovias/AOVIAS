namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Machine-readable rationale stored with every Copilot recommendation.
/// Provides full auditability, enables approval card review, and supports
/// recommendation effectiveness scoring.
/// Serialized as JSON in CopilotRecommendation.RationaleJson.
/// </summary>
public sealed class RecommendationRationale
{
    /// <summary>Name of the rule that generated this recommendation.</summary>
    public string RuleName { get; init; } = string.Empty;

    /// <summary>Key operational metrics that triggered the rule.</summary>
    public Dictionary<string, string> Metrics { get; init; } = new();

    /// <summary>Threshold values compared against (actual vs required).</summary>
    public Dictionary<string, string> Thresholds { get; init; } = new();

    /// <summary>Source entities referenced in the recommendation.</summary>
    public List<RationaleSourceEntity> SourceEntities { get; init; } = new();

    /// <summary>ISO-8601 timestamp of when the data was read.</summary>
    public string DataAsOfUtc { get; init; } = string.Empty;

    /// <summary>Analytics scope used for this evaluation.</summary>
    public string ScopeRef { get; init; } = string.Empty;
}

/// <summary>A single entity reference within a rationale.</summary>
public sealed class RationaleSourceEntity
{
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string EntityRef { get; init; } = string.Empty;
}
