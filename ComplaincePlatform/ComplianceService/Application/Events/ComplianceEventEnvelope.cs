namespace ComplianceService.Application.Events;

/// <summary>
/// Envelope for ComplianceEvent.* outbox rows.
/// <see cref="TenantId"/> is the organization tenant (OrganizationId), never HospitalId.
/// </summary>
public sealed class ComplianceEventEnvelope
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid? UserId { get; init; }
    /// <summary>Organization tenant. Must equal Organization.Hospitals.OrganizationId.</summary>
    public Guid TenantId { get; init; }
    public ComplianceEventType EventType { get; init; }
    public string Payload { get; init; } = "{}";
}
