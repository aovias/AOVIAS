using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Autosave / revision checkpoint for draft recovery (Sprint 15B Phase 13).
/// </summary>
public class DocumentAuthoringCheckpoint : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;
    public Guid DocumentVersionId { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string? Label { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
}
