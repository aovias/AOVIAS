using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Repository for ComplianceJourneyHistory audit trail.
/// </summary>
public interface IComplianceJourneyHistoryRepository
{
    Task<List<ComplianceJourneyHistory>> GetByJourneyIdAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(ComplianceJourneyHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated transition history for a specific hospital and framework,
    /// with optional filtering. Used for enterprise audit reports.
    /// </summary>
    Task<(List<ComplianceJourneyHistory> Items, int TotalCount)> GetTransitionsByHospitalAndFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        ComplianceLifecycleState? fromState = null,
        ComplianceLifecycleState? toState = null,
        Guid? changedBy = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
