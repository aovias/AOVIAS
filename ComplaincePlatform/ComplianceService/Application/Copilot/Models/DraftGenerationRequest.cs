using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Input to the draft generation pipeline.
/// Carries type, source traceability, and optional override instructions.
/// </summary>
public sealed class DraftGenerationRequest
{
    /// <summary>Type of draft to generate (determines template selection).</summary>
    public required DraftType DraftType { get; init; }

    /// <summary>Source that triggered this generation request.</summary>
    public required DraftSourceType SourceType { get; init; }

    /// <summary>Persona context for tone and content targeting.</summary>
    public required string Persona { get; init; }

    /// <summary>Organization scope — all generated content is scoped to this ID.</summary>
    public required Guid OrganizationId { get; init; }

    /// <summary>Source Copilot recommendation ID (if triggered from a recommendation card).</summary>
    public Guid? RecommendationId { get; init; }

    /// <summary>Primary source entity ID (deficiency ID, CAPA ID, etc.).</summary>
    public Guid? SourceEntityId { get; init; }

    /// <summary>Optional human-supplied notes to incorporate into the draft.</summary>
    public string? AdditionalInstructions { get; init; }

    /// <summary>
    /// When set, forces regeneration into a new version under the given group.
    /// If null, a new group (version 1) is created.
    /// </summary>
    public Guid? ExistingGroupId { get; init; }
}
