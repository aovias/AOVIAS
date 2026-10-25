namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when an audit is scheduled for a compliance journey.
/// </summary>
public record ComplianceAuditScheduledDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
