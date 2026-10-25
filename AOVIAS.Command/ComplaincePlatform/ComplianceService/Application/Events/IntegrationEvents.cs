namespace ComplianceService.Application.Events;

/// <summary>
/// Integration events for cross-service communication.
/// These will be published via Gateway (RabbitMQ) but not directly from ComplianceService.
/// </summary>
public record ComplianceApprovedIntegrationEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string CertificateNumber,
    DateTime ExpiryDate,
    DateTime OccurredOn
);

public record ComplianceExpiredIntegrationEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    DateTime ExpiryDate,
    DateTime OccurredOn
);

public record ComplianceSuspendedIntegrationEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string SuspendedReason,
    DateTime OccurredOn
);

public record ComplianceRejectedIntegrationEvent(
    Guid TenantId,
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    Guid ComplianceJourneyId,
    string RejectionReason,
    DateTime OccurredOn
);
