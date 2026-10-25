using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

public class DocumentAcknowledgement : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;
    public Guid DocumentVersionId { get; set; }

    public DocumentAcknowledgementAssigneeType AssigneeType { get; set; }
    public string AssigneeKey { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public Guid? AssigneeUserId { get; set; }

    public DateTime? ReadAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public int ReminderCount { get; set; }
    public DateTime? LastReminderAt { get; set; }
}
