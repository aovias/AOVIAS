using Gateway.DTOs;
using Shared.Infrastructure.DTO;

namespace Gateway.Services;

/// <summary>
/// High-level proxy abstraction for regulatory and framework enrollment operations.
/// This service hides downstream ComplianceService endpoints and shapes responses into ApiResponse&lt;T&gt;.
/// </summary>
public interface IRegulatoryProxyService
{
    Task<ApiResponse<RegulatoryOverviewDto>> GetRegulatoryOverviewAsync(Guid orgId, string traceId, CancellationToken cancellationToken);

    Task<ApiResponse<UpdateJurisdictionResponseDto>> UpdateJurisdictionAsync(
        Guid orgId,
        UpdateJurisdictionRequestDto request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ApiResponse<IReadOnlyList<AccreditationSummaryDto>>> GetAccreditationsAsync(
        Guid orgId,
        string traceId,
        CancellationToken cancellationToken);

    Task<ApiResponse<IReadOnlyList<RenewalDto>>> GetRenewalsAsync(
        Guid orgId,
        int withinDays,
        string traceId,
        CancellationToken cancellationToken);

    Task<ApiResponse<FrameworkCatalogPageDto>> GetFrameworkCatalogAsync(
        Guid orgId,
        string? country,
        string? accreditationBody,
        string? type,
        string? version,
        int page,
        int pageSize,
        string? search,
        string traceId,
        CancellationToken cancellationToken);

    Task<ApiResponse<ActivationMetadataDto>> GetActivationMetadataAsync(
        Guid orgId,
        Guid frameworkId,
        string traceId,
        CancellationToken cancellationToken);

    Task<ApiResponse<ActivateAccreditationResponseDto>> ActivateAccreditationAsync(
        Guid orgId,
        ActivateAccreditationRequestDto request,
        string traceId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AccreditationLifecycleChangeResponseDto>> SuspendAccreditationAsync(
        Guid orgId,
        Guid accreditationId,
        SuspendAccreditationRequestDto request,
        string traceId,
        string? ifMatch = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AccreditationLifecycleChangeResponseDto>> ArchiveAccreditationAsync(
        Guid orgId,
        Guid accreditationId,
        string traceId,
        string? ifMatch = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<RiskRecalculationResponseDto>> RecalculateRiskAsync(
        Guid orgId,
        string traceId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);
}

