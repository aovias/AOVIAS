using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Domain.Copilot;

/// <summary>
/// A first-class business object representing a rule-engine-generated compliance recommendation.
/// Recommendations are organization-scoped from creation for analytics, auditability,
/// and future multi-hospital deployment.
/// Sprint 13B adds deterministic rule-engine generation.
/// Sprint 13C adds draft generation. Sprint 13D adds approval workflows.
/// </summary>
public class CopilotRecommendation : AggregateRoot
{
    // ── Identity ────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }

    // ── Classification ───────────────────────────────────────────────────────
    public string RecommendationType { get; private set; } = string.Empty;
    public RecommendationCategory Category { get; private set; } = RecommendationCategory.Accreditation;
    public RecommendationSeverity Severity { get; private set; } = RecommendationSeverity.Medium;
    public string Persona { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public decimal Confidence { get; private set; } = 1.0m;

    // ── Content ──────────────────────────────────────────────────────────────
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Rationale { get; private set; } = string.Empty;
    public string ExpectedOutcome { get; private set; } = string.Empty;
    public string RiskIfIgnored { get; private set; } = string.Empty;
    public string? SourceReferences { get; private set; }

    // ── Traceability (Sprint 13B) ────────────────────────────────────────────
    /// <summary>Entity type that triggered this recommendation (e.g. "ComplianceDeficiency").</summary>
    public string? SourceEntityType { get; private set; }

    /// <summary>Primary source entity ID for direct navigation.</summary>
    public Guid? SourceEntityId { get; private set; }

    /// <summary>Frontend deep-link URL for one-click navigation.</summary>
    public string? DeepLink { get; private set; }

    /// <summary>Machine-readable rationale as serialized JSON (RecommendationRationale).</summary>
    public string? RationaleJson { get; private set; }

    /// <summary>
    /// Serialized <see cref="RecommendationImpact"/> JSON.
    /// Estimated impact values persisted with the recommendation and surfaced in generated drafts.
    /// </summary>
    public string? ImpactJson { get; private set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    public RecommendationStatus Status { get; private set; } = RecommendationStatus.Proposed;
    public DateTime GeneratedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? DismissedAtUtc { get; private set; }

    // ── EF Core constructor ──────────────────────────────────────────────────
    private CopilotRecommendation() { }

    // ── Factories ────────────────────────────────────────────────────────────

    /// <summary>
    /// Legacy factory — foundation only, no rule engine fields.
    /// Use <see cref="CreateFromRuleEngine"/> for Sprint 13B+ generation.
    /// </summary>
    public static CopilotRecommendation Create(
        Guid organizationId,
        string recommendationType,
        string persona,
        int priority,
        string title,
        string description,
        string rationale,
        string expectedOutcome,
        string riskIfIgnored,
        string? sourceReferences = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(recommendationType))
            throw new ArgumentException("RecommendationType is required.", nameof(recommendationType));

        return new CopilotRecommendation
        {
            Id                 = Guid.NewGuid(),
            OrganizationId     = organizationId,
            RecommendationType = recommendationType,
            Persona            = persona,
            Priority           = priority,
            Title              = title,
            Description        = description,
            Rationale          = rationale,
            ExpectedOutcome    = expectedOutcome,
            RiskIfIgnored      = riskIfIgnored,
            SourceReferences   = sourceReferences,
            Status             = RecommendationStatus.Proposed,
            GeneratedAtUtc     = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Sprint 13B factory — creates a fully-attributed recommendation from the rule engine.
    /// Includes category, severity, confidence, traceability, deep link, and machine-readable rationale.
    /// </summary>
    public static CopilotRecommendation CreateFromRuleEngine(
        Guid organizationId,
        string recommendationType,
        RecommendationCategory category,
        RecommendationSeverity severity,
        string persona,
        int priority,
        decimal confidence,
        string title,
        string description,
        string rationale,
        string expectedOutcome,
        string riskIfIgnored,
        string? sourceReferences,
        string? sourceEntityType,
        Guid? sourceEntityId,
        string? deepLink,
        string? rationaleJson,
        DateTime? expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(recommendationType))
            throw new ArgumentException("RecommendationType is required.", nameof(recommendationType));

        return new CopilotRecommendation
        {
            Id                 = Guid.NewGuid(),
            OrganizationId     = organizationId,
            RecommendationType = recommendationType,
            Category           = category,
            Severity           = severity,
            Persona            = persona,
            Priority           = priority,
            Confidence         = Math.Clamp(confidence, 0m, 1m),
            Title              = title,
            Description        = description,
            Rationale          = rationale,
            ExpectedOutcome    = expectedOutcome,
            RiskIfIgnored      = riskIfIgnored,
            SourceReferences   = sourceReferences,
            SourceEntityType   = sourceEntityType,
            SourceEntityId     = sourceEntityId,
            DeepLink           = deepLink,
            RationaleJson      = rationaleJson,
            Status             = RecommendationStatus.Proposed,
            GeneratedAtUtc     = DateTime.UtcNow,
            ExpiresAtUtc       = expiresAtUtc,
        };
    }

    // ── Sprint 13C Mutations ─────────────────────────────────────────────────

    /// <summary>Attaches estimated impact data to the recommendation (Sprint 13C).</summary>
    public void SetImpact(string impactJson)
    {
        ImpactJson = impactJson;
    }

    // ── State Transitions ────────────────────────────────────────────────────

    /// <summary>User accepted this recommendation.</summary>
    public void Accept()
    {
        if (Status != RecommendationStatus.Proposed)
            throw new InvalidOperationException($"Only Proposed recommendations can be accepted. Current status: {Status}.");

        Status        = RecommendationStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
    }

    /// <summary>User dismissed this recommendation.</summary>
    public void Dismiss()
    {
        if (Status != RecommendationStatus.Proposed)
            throw new InvalidOperationException($"Only Proposed recommendations can be dismissed. Current status: {Status}.");

        Status          = RecommendationStatus.Dismissed;
        DismissedAtUtc  = DateTime.UtcNow;
    }

    /// <summary>System expired this recommendation (e.g., context changed, TTL elapsed).</summary>
    public void Expire()
    {
        if (Status == RecommendationStatus.Expired)
            return;

        Status = RecommendationStatus.Expired;
    }
}
