using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class IncidentTimelineEntry : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
}
