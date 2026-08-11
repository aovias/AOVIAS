using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain.Documentation;

public class DocumentReview : BaseEntity
{
    public Guid ControlledDocumentId { get; set; }
    public ControlledDocument ControlledDocument { get; set; } = null!;
    public Guid? DocumentVersionId { get; set; }

    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public string? ReviewerName { get; set; }
    public string? Outcome { get; set; }
    public string? Notes { get; set; }
}
