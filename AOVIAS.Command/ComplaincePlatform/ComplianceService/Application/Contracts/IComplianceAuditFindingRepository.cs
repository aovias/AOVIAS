using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Repository for unified findings, including audit-scoped lookups.
/// </summary>
public interface IComplianceAuditFindingRepository
{
    Task<List<Finding>> GetByJourneyIdAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default);
    Task<List<Finding>> GetUnresolvedByJourneyIdAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default);
    Task<int> GetMajorNcCountAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default);
    Task<int> GetMinorNcCountAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default);
    Task<bool> HasUnresolvedMajorNcAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default);
    Task<List<Finding>> GetOrganizationFindingsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
    Task<Finding?> GetOrganizationFindingByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default);
    Task AddAsync(Finding finding, CancellationToken cancellationToken = default);
    Task UpdateAsync(Finding finding, CancellationToken cancellationToken = default);
}
