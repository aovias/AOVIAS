using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Review-mode comment on a section or block (Sprint 15B Phase 9).
/// </summary>
public class DocumentAuthoringComment : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;
    public Guid DocumentVersionId { get; set; }
    public Guid? SectionId { get; set; }
    public string? BlockId { get; set; }

    public DocumentAuthoringCommentType CommentType { get; set; } = DocumentAuthoringCommentType.Inline;
    public string Content { get; set; } = string.Empty;
    public Guid? AuthorUserId { get; set; }
    public string? AuthorName { get; set; }
    public string? MentionedUserName { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
