using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Repository for ComplianceJourney aggregate root.
/// </summary>
public interface IComplianceJourneyRepository
{
    Task<ComplianceJourney?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceJourney?> GetByHospitalAndFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);
    Task<List<ComplianceJourney>> GetByHospitalIdAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default);
    Task<List<ComplianceJourney>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
    Task<List<ComplianceJourney>> GetExpiringJourneysAsync(
        DateTime expiryDate,
        CancellationToken cancellationToken = default);
    Task<List<ComplianceJourney>> GetJourneysInRenewalWindowAsync(
        DateTime renewalWindowStart,
        CancellationToken cancellationToken = default);
    Task<List<ComplianceJourney>> GetByStatusAsync(
        ComplianceLifecycleState status,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Batch lookup: returns journeys for multiple hospitals in a single query.
    /// Used by Gateway to enrich organization-detail response without N+1.
    /// </summary>
    Task<List<ComplianceJourney>> GetJourneysByHospitalIdsAsync(
        List<Guid> hospitalIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(ComplianceJourney journey, CancellationToken cancellationToken = default);
    Task UpdateAsync(ComplianceJourney journey, CancellationToken cancellationToken = default);
}
