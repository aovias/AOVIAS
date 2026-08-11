using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Orchestrates data retrieval and calculation for the Compliance Maturity Dashboard.
/// </summary>
public interface IComplianceMaturityAggregationService
{
    /// <summary>
    /// Builds the full Compliance Maturity Dashboard DTO for the given hospital.
    /// </summary>
    /// <param name="hospitalId">Target hospital identifier.</param>
    /// <param name="organizationId">Tenant/organization identifier from JWT.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ComplianceMaturityDashboardDto> GetDashboardAsync(
        Guid hospitalId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
