namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>
/// Request body for POST /copilot/drafts/generate and POST /copilot/drafts/{id}/regenerate.
/// </summary>
public sealed class GenerateDraftRequestDto
{
    /// <summary>Draft type integer value (matches DraftType enum).</summary>
    public int DraftType { get; init; }

    /// <summary>Source type integer value (matches DraftSourceType enum).</summary>
    public int SourceType { get; init; }

    /// <summary>Persona name to scope the draft tone and content.</summary>
    public string Persona { get; init; } = string.Empty;

    /// <summary>Source recommendation ID when generating from a recommendation card.</summary>
    public Guid? RecommendationId { get; init; }

    /// <summary>Primary source entity ID (deficiency, CAPA, etc.).</summary>
    public Guid? SourceEntityId { get; init; }

    /// <summary>Optional human-supplied notes to incorporate into the draft.</summary>
    public string? AdditionalInstructions { get; init; }
}
