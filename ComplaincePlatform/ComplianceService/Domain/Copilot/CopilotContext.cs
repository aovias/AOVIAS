namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Captures the runtime context of a Copilot session.
/// Separate from AIContextBundle — can compose it later when generation logic is added.
/// Passed to persona routing, recommendation generation (13B), and draft generation (13C).
/// </summary>
public sealed class CopilotContext
{
    /// <summary>Tenant / hospital organization for isolation.</summary>
    public Guid OrganizationId { get; init; }

    /// <summary>Matches the page context strings from AiPageContextService on the frontend.</summary>
    public string PageContext { get; init; } = string.Empty;

    /// <summary>Entity currently in focus (ObjectiveElement, Deficiency, CAPA ID etc.).</summary>
    public Guid? FocusedEntityId { get; init; }

    /// <summary>Type label of the focused entity (e.g. "ObjectiveElement", "Deficiency").</summary>
    public string? FocusedEntityType { get; init; }

    /// <summary>Rolling summary of the current conversation thread.</summary>
    public string? ConversationSummary { get; init; }

    /// <summary>Recently accessed entity references (max 8, same as AI context service).</summary>
    public IReadOnlyList<string> RecentEntityRefs { get; init; } = Array.Empty<string>();

    /// <summary>Persona active for this session (resolved by router or manual override).</summary>
    public CopilotPersona ActivePersona { get; init; } = CopilotPersona.AccreditationCoach;

    /// <summary>Manual persona override from the user. Null means auto-route.</summary>
    public CopilotPersona? PersonaOverride { get; init; }

    /// <summary>Current maturity stage label (e.g. "Core", "Commitment").</summary>
    public string? CurrentStage { get; init; }

    /// <summary>Latest readiness score (0-100) for the organization's active framework.</summary>
    public decimal? ReadinessScore { get; init; }

    /// <summary>Condensed analytics summary string for context injection.</summary>
    public string? AnalyticsSummary { get; init; }

    /// <summary>JWT / session role claims used when page/entity signals are absent.</summary>
    public IReadOnlyList<string> UserRoles { get; init; } = Array.Empty<string>();
}
