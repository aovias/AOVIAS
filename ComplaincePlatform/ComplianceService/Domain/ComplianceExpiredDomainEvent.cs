namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when compliance certification expires.
/// </summary>
public record ComplianceExpiredDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    DateTime ExpiryDate
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
