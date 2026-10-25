using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Read-side service for OrganizationComplianceObligation projections.
/// </summary>
public interface IObligationQueryService
{
    /// <summary>Returns all obligations for the organization × framework, with optional status filters.</summary>
    Task<IReadOnlyList<OrganizationObligationDto>> GetObligationsAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default);

    /// <summary>Returns a summary with totals and maturity breakdown counts.</summary>
    Task<OrganizationObligationSummaryDto> GetSummaryAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default);

    /// <summary>Returns per-band (Core/Commitment/Achievement/Excellence) obligation counts and scores.</summary>
    Task<ObligationMaturityBreakdownDto> GetMaturityBreakdownAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default);

    /// <summary>Returns per-chapter readiness projection for the organization × framework.</summary>
    Task<IReadOnlyList<ObligationReadinessProjectionDto>> GetReadinessProjectionAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default);
}
