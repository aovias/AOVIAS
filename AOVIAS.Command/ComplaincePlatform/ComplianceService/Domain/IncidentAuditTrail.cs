using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class IncidentAuditTrail : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string ActorType { get; set; } = "User";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Detail { get; set; }
}
