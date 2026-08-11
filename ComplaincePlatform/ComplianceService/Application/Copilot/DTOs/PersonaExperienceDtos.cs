namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Sprint 13E — persona experience definition for API responses.</summary>
public sealed class PersonaExperienceDefinitionDto
{
    public string Persona { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public IReadOnlyList<string> PrimaryObjectives { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];
    public IReadOnlyList<string> DefaultQuestions { get; init; } = [];
    public IReadOnlyList<string> PriorityCategories { get; init; } = [];
    public IReadOnlyList<int> SupportedDraftTypes { get; init; } = [];
    public IReadOnlyList<int> SupportedProposalTypes { get; init; } = [];
}

/// <summary>Sprint 13E — contextual landing headline for a persona home state.</summary>
public sealed class PersonaLandingStateDto
{
    public string Persona { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string Subheadline { get; init; } = string.Empty;
    public string? FocusArea { get; init; }
    public decimal? ReadinessScore { get; init; }
    public string? TopRecommendationTitle { get; init; }
    public string? KeyKpiLabel { get; init; }
    public string? KeyKpiValue { get; init; }
}

/// <summary>Sprint 13E — full persona workspace bundle.</summary>
public sealed class PersonaWorkspaceDto
{
    public PersonaExperienceDefinitionDto Definition { get; init; } = new();
    public PersonaLandingStateDto Landing { get; init; } = new();
    public IReadOnlyList<CopilotRecommendationDto> PrioritizedRecommendations { get; init; } = [];
    public IReadOnlyList<AiDraftDto> HighlightedDrafts { get; init; } = [];
    public IReadOnlyList<CopilotActionProposalDto> HighlightedProposals { get; init; } = [];
    public IReadOnlyList<string> SuggestedQuestions { get; init; } = [];
}

/// <summary>Sprint 13E — read-only activity feed entry.</summary>
public sealed class CopilotActivityEventDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? Persona { get; init; }
    public string? ActorId { get; init; }
    public string? ActorEmail { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? Notes { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

/// <summary>Sprint 13E — aggregated usage metrics (stored events, no dashboard yet).</summary>
public sealed class CopilotUsageMetricsDto
{
    public IReadOnlyDictionary<string, int> PersonaUsageCounts { get; init; } = new Dictionary<string, int>();
    public int DraftsGenerated { get; init; }
    public int ProposalsApproved { get; init; }
    public int ProposalsExecuted { get; init; }
    public int RecommendationsEngaged { get; init; }
    public int WorkspaceViews { get; init; }
    public int PersonaChanges { get; init; }
    public DateTime? MetricsAsOfUtc { get; init; }
}

/// <summary>Request to record a client-side copilot activity event.</summary>
public sealed class RecordCopilotActivityRequestDto
{
    public string EventType { get; init; } = string.Empty;
    public string? Persona { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? Notes { get; init; }
}
