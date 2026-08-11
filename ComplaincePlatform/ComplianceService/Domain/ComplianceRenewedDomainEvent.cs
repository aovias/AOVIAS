namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised when compliance is renewed.
/// </summary>
public record ComplianceRenewedDomainEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string CertificateNumber,
    DateTime ExpiryDate,
    DateTime? PreviousApprovalDate
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
