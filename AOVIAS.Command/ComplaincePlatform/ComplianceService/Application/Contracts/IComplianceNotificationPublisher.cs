namespace ComplianceService.Application.Contracts;

/// <summary>
/// Lightweight in-process notification publisher for Phase 1 domain events.
/// Channel may be console / in-app; CommunicationService wiring can replace the implementation later.
/// </summary>
public interface IComplianceNotificationPublisher
{
    Task PublishAsync(ComplianceNotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed class ComplianceNotificationMessage
{
    public required string EventType { get; init; }
    public required string Title { get; init; }
    public string? Body { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string? RecipientUserId { get; init; }
    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
