namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Read model returned by the Copilot recommendations API.</summary>
public sealed class CopilotRecommendationDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string RecommendationType { get; init; } = string.Empty;

    // ── Taxonomy (Sprint 13B) ─────────────────────────────────────────────────
    public string Category { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public decimal Confidence { get; init; }

    public string Persona { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
    public string ExpectedOutcome { get; init; } = string.Empty;
    public string RiskIfIgnored { get; init; } = string.Empty;
    public string? SourceReferences { get; init; }

    // ── Traceability (Sprint 13B) ─────────────────────────────────────────────
    public string? SourceEntityType { get; init; }
    public Guid? SourceEntityId { get; init; }
    public string? DeepLink { get; init; }

    // ── Machine-readable rationale (Sprint 13B) ──────────────────────────────
    /// <summary>Serialized RecommendationRationale — metrics, thresholds, source entities, rule name.</summary>
    public string? RationaleJson { get; init; }

    // ── Impact estimation (Sprint 13C) ────────────────────────────────────────
    /// <summary>Serialized RecommendationImpact — estimated readiness, CMI, deficiency, SLA, CAPA impact.</summary>
    public string? ImpactJson { get; init; }

    /// <summary>Draft action label for the "Generate Draft" button on the recommendation card.</summary>
    public string? SuggestedDraftAction { get; init; }

    public string Status { get; init; } = string.Empty;
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? DismissedAtUtc { get; init; }
}
