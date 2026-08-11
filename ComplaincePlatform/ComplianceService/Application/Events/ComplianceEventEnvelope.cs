namespace ComplianceService.Application.Events;

public sealed class ComplianceEventEnvelope
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid? UserId { get; init; }
    public Guid TenantId { get; init; }
    public ComplianceEventType EventType { get; init; }
    public string Payload { get; init; } = "{}";
}
