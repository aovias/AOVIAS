using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

/// <summary>
/// Append-only audit trail for every lifecycle transition and significant action.
/// </summary>
public class DocumentLifecycleAuditEntry : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;
    public Guid? DocumentVersionId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public ControlledDocumentStatus? FromStatus { get; set; }
    public ControlledDocumentStatus? ToStatus { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
    public string? Details { get; set; }
}
