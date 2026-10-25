using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

public sealed class GetDeficiencyAssignmentSuggestionQueryHandler : IRequestHandler<GetDeficiencyAssignmentSuggestionQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetDeficiencyAssignmentSuggestionQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetDeficiencyAssignmentSuggestionQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/assignment/deficiencies/{request.DeficiencyId}/suggestion",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class AutoAssignDeficiencyCommandHandler : IRequestHandler<AutoAssignDeficiencyCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AutoAssignDeficiencyCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(AutoAssignDeficiencyCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/assignment/deficiencies/{request.DeficiencyId}/auto-assign?force={request.Force.ToString().ToLowerInvariant()}",
            HttpMethod.Post, null, null, cancellationToken);
}

public sealed class GetFindingAssignmentSuggestionQueryHandler : IRequestHandler<GetFindingAssignmentSuggestionQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetFindingAssignmentSuggestionQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetFindingAssignmentSuggestionQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/assignment/findings/{request.FindingId}/suggestion?organizationId={request.OrganizationId}&hospitalId={request.HospitalId}",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetAssignmentRoutingMetricsQueryHandler : IRequestHandler<GetAssignmentRoutingMetricsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetAssignmentRoutingMetricsQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetAssignmentRoutingMetricsQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/assignment/metrics?organizationId={request.OrganizationId}",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetDepartmentLabelMappingsQueryHandler : IRequestHandler<GetDepartmentLabelMappingsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetDepartmentLabelMappingsQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetDepartmentLabelMappingsQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            "/internal/v1/assignment/department-label-mappings",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class ListDepartmentOwnershipQueryHandler : IRequestHandler<ListDepartmentOwnershipQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ListDepartmentOwnershipQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ListDepartmentOwnershipQuery request, CancellationToken cancellationToken)
    {
        var hospital = request.HospitalId.HasValue ? $"&hospitalId={request.HospitalId}" : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync(
            $"/internal/v1/assignment/department-ownership?organizationId={request.OrganizationId}{hospital}",
            HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class UpsertDepartmentOwnershipCommandHandler : IRequestHandler<UpsertDepartmentOwnershipCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public UpsertDepartmentOwnershipCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(UpsertDepartmentOwnershipCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            "/internal/v1/assignment/department-ownership",
            HttpMethod.Put, request.Body, null, cancellationToken);
}
