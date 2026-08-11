namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Structured Source Intelligence metadata embedded in every generated draft.
/// Serialized to JSON and stored in <see cref="AiDraft.MetadataJson"/>.
/// Enables auditability, explainability, and reliable regeneration.
/// </summary>
public sealed class DraftSourceIntelligence
{
    /// <summary>IDs of CopilotRecommendations that contributed to this draft.</summary>
    public List<string> RecommendationIds { get; init; } = [];

    /// <summary>KPI name → value snapshot at generation time (e.g., "ReadinessScore" → "74.2").</summary>
    public Dictionary<string, string> KpiValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Deficiency IDs or codes referenced during generation.</summary>
    public List<string> DeficiencyRefs { get; init; } = [];

    /// <summary>CAPA IDs or codes referenced during generation.</summary>
    public List<string> CapaRefs { get; init; } = [];

    /// <summary>Obligation IDs or codes referenced during generation.</summary>
    public List<string> ObligationRefs { get; init; } = [];

    /// <summary>Analytics metric name → value snapshot (e.g., "OverdueCAPAs" → "12").</summary>
    public Dictionary<string, string> AnalyticsMetrics { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Serialized rationale objects (JSON snippets) from source recommendations.</summary>
    public List<string> RationaleObjects { get; init; } = [];

    /// <summary>The template name used to generate the draft.</summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>The generator that produced the content (e.g., "TemplateDraftContentGenerator").</summary>
    public string GeneratorName { get; init; } = string.Empty;

    /// <summary>OrganizationId scope under which the draft was generated.</summary>
    public string OrganizationId { get; init; } = string.Empty;

    /// <summary>UTC timestamp when this intelligence snapshot was captured.</summary>
    public string GeneratedAtUtc { get; init; } = DateTime.UtcNow.ToString("O");

    /// <summary>Schema version for forward-compatible deserialization.</summary>
    public string SchemaVersion { get; init; } = "1.0";
}
