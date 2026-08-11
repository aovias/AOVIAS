using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Auditor;

/// <summary>
/// Auditor-hospital conversation attached to an audit session.
/// </summary>
public class AuditComment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid AuditSessionId { get; set; }
    public AuditSession? AuditSession { get; set; }

    public AuditCommentType Type { get; set; } = AuditCommentType.GeneralComment;
    public string Content { get; set; } = string.Empty;

    public Guid AuthorUserId { get; set; }
    public string? AuthorName { get; set; }
    public bool IsAuditorAuthor { get; set; }

    public Guid? ParentCommentId { get; set; }
    public AuditComment? ParentComment { get; set; }
    public ICollection<AuditComment> Replies { get; set; } = new List<AuditComment>();
}
