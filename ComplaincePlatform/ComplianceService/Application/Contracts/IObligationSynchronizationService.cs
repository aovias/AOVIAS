namespace ComplianceService.Application.Contracts;

/// <summary>
/// Idempotent synchronization of OrganizationComplianceObligation records for an organization.
/// Creates one obligation per ObjectiveElement in the framework that does not already exist.
/// Never deletes or modifies existing obligations.
/// </summary>
public interface IObligationSynchronizationService
{
    /// <summary>
    /// Ensures every ObjectiveElement in <paramref name="frameworkId"/> has a corresponding
    /// OrganizationComplianceObligation for <paramref name="organizationId"/>.
    /// Safe to call multiple times — idempotent.
    /// </summary>
    Task SyncForOrganizationAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid tenantId,
        Guid frameworkId,
        CancellationToken ct = default);
}
