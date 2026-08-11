namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when compliance is suspended.
/// </summary>
public record ComplianceSuspendedDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string SuspendedReason
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
