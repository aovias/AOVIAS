namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when renewal process begins.
/// </summary>
public record ComplianceRenewalInProgressDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
