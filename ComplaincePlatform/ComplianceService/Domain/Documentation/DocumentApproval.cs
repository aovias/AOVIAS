using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Immutable approval record in the configurable approval chain.
/// </summary>
public class DocumentApproval : BaseEntity
{
    public Guid DocumentVersionId { get; set; }
    public DocumentVersion DocumentVersion { get; set; } = null!;

    public int ApprovalOrder { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public Guid? ApproverUserId { get; set; }
    public string? ApproverName { get; set; }
    public DocumentApprovalStatus Status { get; set; } = DocumentApprovalStatus.Pending;
    public DateTime? DecidedAt { get; set; }
    public string? Comments { get; set; }
}
