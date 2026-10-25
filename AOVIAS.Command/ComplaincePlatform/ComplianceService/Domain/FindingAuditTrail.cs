using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class FindingAuditTrail : BaseEntity
{
    public Guid FindingId { get; set; }
    public Finding Finding { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string ActorType { get; set; } = "User";
    public string? PreviousStateJson { get; set; }
    public string? NewStateJson { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
