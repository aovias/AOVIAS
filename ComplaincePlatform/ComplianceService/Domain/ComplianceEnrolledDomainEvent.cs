namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when a hospital enrolls in a compliance framework.
/// </summary>
public record ComplianceEnrolledDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
