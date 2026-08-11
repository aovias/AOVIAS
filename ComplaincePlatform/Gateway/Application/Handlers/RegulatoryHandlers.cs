using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for GetRegulatoryOverviewQuery.
/// Business logic such as aggregating accreditations, renewals, and risk index
/// is executed inside ComplianceService; this handler only orchestrates the call.
/// </summary>
public class GetRegulatoryOverviewQueryHandler
    : IRequestHandler<GetRegulatoryOverviewQuery, Shared.Infrastructure.DTO.ApiResponse<RegulatoryOverviewDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public GetRegulatoryOverviewQueryHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<RegulatoryOverviewDto>> Handle(
        GetRegulatoryOverviewQuery request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.GetRegulatoryOverviewAsync(request.OrgId, request.TraceId, cancellationToken);
    }
}

/// <summary>
/// Handler for UpdateJurisdictionCommand.
/// Downstream service enforces business rules such as preventing changes when
/// active accreditations exist and logging audit entries / cache refresh triggers.
/// </summary>
public class UpdateJurisdictionCommandHandler
    : IRequestHandler<UpdateJurisdictionCommand, Shared.Infrastructure.DTO.ApiResponse<UpdateJurisdictionResponseDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public UpdateJurisdictionCommandHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<UpdateJurisdictionResponseDto>> Handle(
        UpdateJurisdictionCommand request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.UpdateJurisdictionAsync(request.OrgId, request.Request, request.TraceId, cancellationToken);
    }
}

/// <summary>
/// Handler for GetAccreditationsQuery.
/// </summary>
public class GetAccreditationsQueryHandler
    : IRequestHandler<GetAccreditationsQuery, Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<AccreditationSummaryDto>>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public GetAccreditationsQueryHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<AccreditationSummaryDto>>> Handle(
        GetAccreditationsQuery request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.GetAccreditationsAsync(request.OrgId, request.TraceId, cancellationToken);
    }
}

/// <summary>
/// Handler for GetRenewalsQuery.
/// </summary>
public class GetRenewalsQueryHandler
    : IRequestHandler<GetRenewalsQuery, Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<RenewalDto>>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public GetRenewalsQueryHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<IReadOnlyList<RenewalDto>>> Handle(
        GetRenewalsQuery request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.GetRenewalsAsync(request.OrgId, request.WithinDays, request.TraceId, cancellationToken);
    }
}

/// <summary>
/// Handler for GetFrameworkCatalogQuery.
/// </summary>
public class GetFrameworkCatalogQueryHandler
    : IRequestHandler<GetFrameworkCatalogQuery, Shared.Infrastructure.DTO.ApiResponse<FrameworkCatalogPageDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public GetFrameworkCatalogQueryHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<FrameworkCatalogPageDto>> Handle(
        GetFrameworkCatalogQuery request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.GetFrameworkCatalogAsync(
            request.OrgId,
            request.Country,
            request.AccreditationBody,
            request.Type,
            request.Version,
            request.Page,
            request.PageSize,
            request.Search,
            request.TraceId,
            cancellationToken);
    }
}

/// <summary>
/// Handler for GetActivationMetadataQuery.
/// </summary>
public class GetActivationMetadataQueryHandler
    : IRequestHandler<GetActivationMetadataQuery, Shared.Infrastructure.DTO.ApiResponse<ActivationMetadataDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public GetActivationMetadataQueryHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<ActivationMetadataDto>> Handle(
        GetActivationMetadataQuery request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.GetActivationMetadataAsync(
            request.OrgId,
            request.FrameworkId,
            request.TraceId,
            cancellationToken);
    }
}

/// <summary>
/// Handler for ActivateAccreditationCommand.
/// </summary>
public class ActivateAccreditationCommandHandler
    : IRequestHandler<ActivateAccreditationCommand, Shared.Infrastructure.DTO.ApiResponse<ActivateAccreditationResponseDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public ActivateAccreditationCommandHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<ActivateAccreditationResponseDto>> Handle(
        ActivateAccreditationCommand request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.ActivateAccreditationAsync(
            request.OrgId,
            request.Request,
            request.TraceId,
            request.IdempotencyKey,
            cancellationToken);
    }
}

/// <summary>
/// Handler for SuspendAccreditationCommand.
/// Downstream service ensures that only active accreditations can be suspended.
/// </summary>
public class SuspendAccreditationCommandHandler
    : IRequestHandler<SuspendAccreditationCommand, Shared.Infrastructure.DTO.ApiResponse<AccreditationLifecycleChangeResponseDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public SuspendAccreditationCommandHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<AccreditationLifecycleChangeResponseDto>> Handle(
        SuspendAccreditationCommand request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.SuspendAccreditationAsync(
            request.OrgId,
            request.AccreditationId,
            request.Request,
            request.TraceId,
            request.IfMatch,
            cancellationToken);
    }
}

/// <summary>
/// Handler for ArchiveAccreditationCommand.
/// Downstream service enforces that only expired accreditations can be archived.
/// </summary>
public class ArchiveAccreditationCommandHandler
    : IRequestHandler<ArchiveAccreditationCommand, Shared.Infrastructure.DTO.ApiResponse<AccreditationLifecycleChangeResponseDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public ArchiveAccreditationCommandHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<AccreditationLifecycleChangeResponseDto>> Handle(
        ArchiveAccreditationCommand request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.ArchiveAccreditationAsync(
            request.OrgId,
            request.AccreditationId,
            request.TraceId,
            request.IfMatch,
            cancellationToken);
    }
}

/// <summary>
/// Handler for RecalculateRiskCommand.
/// Downstream ComplianceService schedules background job and returns accepted status.
/// </summary>
public class RecalculateRiskCommandHandler
    : IRequestHandler<RecalculateRiskCommand, Shared.Infrastructure.DTO.ApiResponse<RiskRecalculationResponseDto>>
{
    private readonly IRegulatoryProxyService _regulatoryProxy;

    public RecalculateRiskCommandHandler(IRegulatoryProxyService regulatoryProxy)
    {
        _regulatoryProxy = regulatoryProxy;
    }

    public Task<Shared.Infrastructure.DTO.ApiResponse<RiskRecalculationResponseDto>> Handle(
        RecalculateRiskCommand request,
        CancellationToken cancellationToken)
    {
        return _regulatoryProxy.RecalculateRiskAsync(
            request.OrgId,
            request.TraceId,
            request.IdempotencyKey,
            cancellationToken);
    }
}

