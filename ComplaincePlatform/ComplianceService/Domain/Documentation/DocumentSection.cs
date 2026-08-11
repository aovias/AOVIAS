using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// First-class structured section within a document version (Sprint 15B).
/// Blocks are stored as JSON — not a single HTML blob.
/// </summary>
public class DocumentSection : BaseEntity
{
    public Guid DocumentVersionId { get; set; }
    public DocumentVersion DocumentVersion { get; set; } = null!;

    public string SectionKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCollapsed { get; set; }

    /// <summary>JSON array of authoring blocks (headings, paragraphs, callouts, etc.).</summary>
    public string BlocksJson { get; set; } = "[]";

    public DateTime LastEditedAt { get; set; } = DateTime.UtcNow;
}
