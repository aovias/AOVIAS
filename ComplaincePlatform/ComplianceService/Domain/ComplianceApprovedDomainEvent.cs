namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when compliance is approved/certified.
/// </summary>
public record ComplianceApprovedDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string CertificateNumber,
    DateTime ExpiryDate
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
