namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when an audit is completed.
/// </summary>
public record ComplianceAuditCompletedDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
