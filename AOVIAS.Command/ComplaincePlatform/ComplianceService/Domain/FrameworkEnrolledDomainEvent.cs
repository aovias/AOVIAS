namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when a hospital enrolls in a compliance framework.
/// </summary>
public record FrameworkEnrolledDomainEvent(
    Guid HospitalId,
    Guid FrameworkId,
    Guid RequestedByUserId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
