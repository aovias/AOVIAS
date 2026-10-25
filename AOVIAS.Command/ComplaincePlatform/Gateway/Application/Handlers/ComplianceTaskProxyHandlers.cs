using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class ListComplianceTasksQueryHandler : IRequestHandler<ListComplianceTasksQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public ListComplianceTasksQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(ListComplianceTasksQuery request, CancellationToken cancellationToken)
    {
        var q = request.FrameworkId is { } fid && fid != Guid.Empty
            ? $"?frameworkId={fid}"
            : string.Empty;
        var endpoint = $"/api/v{request.ApiVersion}/compliance/compliance-tasks{q}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, cancellationToken: cancellationToken);
    }
}

public sealed class GetComplianceTaskByIdQueryHandler : IRequestHandler<GetComplianceTaskByIdQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetComplianceTaskByIdQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetComplianceTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/compliance/compliance-tasks/{request.TaskId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, cancellationToken: cancellationToken);
    }
}

public sealed class CreateComplianceTaskCommandHandler : IRequestHandler<CreateComplianceTaskCommand, IActionResult>
{
    private readonly IProxyService _proxyService;

    public CreateComplianceTaskCommandHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(CreateComplianceTaskCommand request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/compliance/compliance-tasks";
        return _proxyService.ProxyToComplianceServiceAsync(
            endpoint,
            HttpMethod.Post,
            request.RequestBody,
            cancellationToken: cancellationToken);
    }
}

public sealed class PatchComplianceTaskCommandHandler : IRequestHandler<PatchComplianceTaskCommand, IActionResult>
{
    private readonly IProxyService _proxyService;

    public PatchComplianceTaskCommandHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(PatchComplianceTaskCommand request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/compliance/compliance-tasks/{request.TaskId}";
        return _proxyService.ProxyToComplianceServiceAsync(
            endpoint,
            HttpMethod.Patch,
            request.RequestBody,
            cancellationToken: cancellationToken);
    }
}

public sealed class CompleteComplianceTaskCommandHandler : IRequestHandler<CompleteComplianceTaskCommand, IActionResult>
{
    private readonly IProxyService _proxyService;

    public CompleteComplianceTaskCommandHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(CompleteComplianceTaskCommand request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/compliance/frameworks/{request.FrameworkId}/tasks/{request.TaskId}/complete";
        return _proxyService.ProxyToComplianceServiceAsync(
            endpoint,
            HttpMethod.Put,
            null,
            cancellationToken: cancellationToken);
    }
}
