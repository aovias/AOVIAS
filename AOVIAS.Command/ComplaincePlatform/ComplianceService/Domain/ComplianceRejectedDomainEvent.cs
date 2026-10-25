namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when compliance submission is rejected.
/// </summary>
public record ComplianceRejectedDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string RejectionReason
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
