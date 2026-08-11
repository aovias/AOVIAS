using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace AIOrchestratorService.Domain;

/// <summary>
/// Represents an AI-generated draft artifact.
/// This entity is NOT a compliance or audit entity and must never overwrite core entities automatically.
/// </summary>
public class DraftArtifact : BaseEntity
{
    /// <summary>
    /// The type of draft (e.g., ComplianceNarrativeDraft, AuditResponseDraft, CorrectiveActionDraft).
    /// </summary>
    public DraftType DraftType { get; set; }

    /// <summary>
    /// The ID of the source entity this draft is associated with (e.g., AuditFindingId, ComplianceTaskId).
    /// </summary>
    public Guid SourceEntityId { get; set; }

    /// <summary>
    /// The generated content of the draft.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// AI reasoning and explainability information for how this draft was generated.
    /// </summary>
    public ExplainabilityDto? Explainability { get; set; }

    /// <summary>
    /// The AI model or system that generated this draft (e.g., "GPT-4", "Claude-3", "System").
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the draft. Defaults to Draft.
    /// </summary>
    public DraftStatus Status { get; set; } = DraftStatus.Draft;

    /// <summary>
    /// The timestamp when this draft was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}
