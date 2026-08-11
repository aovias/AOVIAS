namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when compliance documentation is submitted for review.
/// </summary>
public record ComplianceSubmittedDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    decimal ReadinessScore
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
