namespace ComplianceService.Domain;

/// <summary>
/// Represents a detected compliance gap.
/// Deterministic and explainable - no AI, no scoring, no prioritization.
/// </summary>
public class Gap
{
    /// <summary>
    /// The type of gap detected.
    /// </summary>
    public GapType GapType { get; set; }

    /// <summary>
    /// Human-readable description of the gap.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The type of entity that is affected by this gap.
    /// </summary>
    public TargetEntityType AffectedEntityType { get; set; }

    /// <summary>
    /// The identifier of the affected entity.
    /// </summary>
    public Guid? AffectedEntityId { get; set; }

    /// <summary>
    /// List of rules that were referenced when detecting this gap.
    /// Optional - only included if rules were explicitly applied.
    /// </summary>
    public List<string> RulesReferenced { get; set; } = new();
}
