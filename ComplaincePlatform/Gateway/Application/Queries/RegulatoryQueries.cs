using Gateway.DTOs;
using MediatR;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Queries;

/// <summary>
/// Query for organization-level regulatory overview.
/// First layer validation is applied via FluentValidation.
/// Handler orchestrates downstream call to ComplianceService via IRegulatoryProxyService.
/// </summary>
public sealed record GetRegulatoryOverviewQuery(Guid OrgId, string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<RegulatoryOverviewDto>>;

/// <summary>
/// Query for fetching active accreditations for an organization.
/// </summary>
public sealed record GetAccreditationsQuery(Guid OrgId, string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<AccreditationSummaryDto>>>;

/// <summary>
/// Query for fetching upcoming renewals within a time window.
/// </summary>
public sealed record GetRenewalsQuery(Guid OrgId, int WithinDays, string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<RenewalDto>>>;

/// <summary>
/// Query for framework catalog for an organization with jurisdictional filters.
/// </summary>
public sealed record GetFrameworkCatalogQuery(
    Guid OrgId,
    string? Country,
    string? AccreditationBody,
    string? Type,
    string? Version,
    int Page,
    int PageSize,
    string? Search,
    string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<FrameworkCatalogPageDto>>;

/// <summary>
/// Query for framework activation metadata.
/// </summary>
public sealed record GetActivationMetadataQuery(Guid OrgId, Guid FrameworkId, string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<ActivationMetadataDto>>;

/// <summary>
/// Query for fetching active departments.
/// </summary>
public sealed record GetDepartmentsQuery(string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<DepartmentDto>>>;

/// <summary>
/// Query for fetching active services.
/// </summary>
public sealed record GetServicesQuery(Guid? DepartmentId, string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<ServiceDto>>>;

/// <summary>
/// Query for fetching master departments.
/// </summary>
public sealed record GetMasterDepartmentsQuery(string TraceId)
    : IRequest<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<MasterDepartmentDto>>>;