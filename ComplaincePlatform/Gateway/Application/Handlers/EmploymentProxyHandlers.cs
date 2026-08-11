using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class ListEmploymentsQueryHandler : IRequestHandler<ListEmploymentsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ListEmploymentsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(ListEmploymentsQuery request, CancellationToken cancellationToken)
    {
        var q = $"?page={request.Page}&pageSize={request.PageSize}";
        if (request.OrganizationId.HasValue) q += $"&organizationId={request.OrganizationId}";
        if (request.HospitalId.HasValue) q += $"&hospitalId={request.HospitalId}";
        if (request.StatusValueId.HasValue) q += $"&statusValueId={request.StatusValueId}";
        return _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class SearchEmploymentsQueryHandler : IRequestHandler<SearchEmploymentsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public SearchEmploymentsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(SearchEmploymentsQuery request, CancellationToken cancellationToken)
    {
        var q = $"?q={Uri.EscapeDataString(request.Query)}&page={request.Page}&pageSize={request.PageSize}";
        return _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/search{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class GetEmploymentByIdQueryHandler : IRequestHandler<GetEmploymentByIdQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetEmploymentByIdQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetEmploymentByIdQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class CreateEmploymentCommandHandler : IRequestHandler<CreateEmploymentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public CreateEmploymentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(CreateEmploymentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class UpdateEmploymentCommandHandler : IRequestHandler<UpdateEmploymentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public UpdateEmploymentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(UpdateEmploymentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}", HttpMethod.Put, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class DeleteEmploymentCommandHandler : IRequestHandler<DeleteEmploymentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public DeleteEmploymentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(DeleteEmploymentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}", HttpMethod.Delete, cancellationToken: cancellationToken);
}

public sealed class AssignPositionCommandHandler : IRequestHandler<AssignPositionCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AssignPositionCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AssignPositionCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/assign-position", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AssignDepartmentCommandHandler : IRequestHandler<AssignDepartmentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AssignDepartmentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AssignDepartmentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/assign-department", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class AssignManagerCommandHandler : IRequestHandler<AssignManagerCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public AssignManagerCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(AssignManagerCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/assign-manager", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class TransferEmploymentCommandHandler : IRequestHandler<TransferEmploymentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public TransferEmploymentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(TransferEmploymentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/transfer", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class PromoteEmploymentCommandHandler : IRequestHandler<PromoteEmploymentCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public PromoteEmploymentCommandHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(PromoteEmploymentCommand request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/promote", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
}

public sealed class GetEmploymentHistoryQueryHandler : IRequestHandler<GetEmploymentHistoryQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetEmploymentHistoryQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetEmploymentHistoryQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/history", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetEmploymentTimelineQueryHandler : IRequestHandler<GetEmploymentTimelineQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetEmploymentTimelineQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetEmploymentTimelineQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/{request.EmploymentId}/timeline", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetWorkforceLookupsQueryHandler : IRequestHandler<GetWorkforceLookupsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetWorkforceLookupsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetWorkforceLookupsQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/lookups/{Uri.EscapeDataString(request.Category)}", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetOrgChartQueryHandler : IRequestHandler<GetOrgChartQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetOrgChartQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetOrgChartQuery request, CancellationToken cancellationToken)
    {
        var q = $"?organizationId={request.OrganizationId}";
        if (request.HospitalId.HasValue) q += $"&hospitalId={request.HospitalId}";
        if (request.RootEmploymentId.HasValue) q += $"&rootEmploymentId={request.RootEmploymentId}";
        return _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/org-chart{q}", HttpMethod.Get, cancellationToken: cancellationToken);
    }
}

public sealed class GetManagerTeamQueryHandler : IRequestHandler<GetManagerTeamQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetManagerTeamQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetManagerTeamQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/manager/{request.ManagerEmploymentId}/team", HttpMethod.Get, cancellationToken: cancellationToken);
}

public sealed class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetPositionsQueryHandler(IProxyService proxy) => _proxy = proxy;
    public Task<IActionResult> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
        => _proxy.ProxyToWorkforceServiceAsync($"/api/v{request.ApiVersion}/employment/positions?organizationId={request.OrganizationId}", HttpMethod.Get, cancellationToken: cancellationToken);
}
