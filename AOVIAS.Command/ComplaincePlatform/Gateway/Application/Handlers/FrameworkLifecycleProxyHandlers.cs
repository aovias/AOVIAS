using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class GetComplianceFrameworkCatalogQueryHandler
    : IRequestHandler<GetComplianceFrameworkCatalogQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetComplianceFrameworkCatalogQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetComplianceFrameworkCatalogQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/catalog?includeInactive={request.IncludeInactive.ToString().ToLowerInvariant()}",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class PublishFrameworkCommandHandler : IRequestHandler<PublishFrameworkCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public PublishFrameworkCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(PublishFrameworkCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/{request.FrameworkId}/publish",
            HttpMethod.Post, null, null, cancellationToken);
}

public sealed class ActivateFrameworkCommandHandler : IRequestHandler<ActivateFrameworkCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public ActivateFrameworkCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ActivateFrameworkCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/{request.FrameworkId}/activate",
            HttpMethod.Post, null, null, cancellationToken);
}

public sealed class RetireFrameworkCommandHandler : IRequestHandler<RetireFrameworkCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public RetireFrameworkCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(RetireFrameworkCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/{request.FrameworkId}/retire",
            HttpMethod.Post, null, null, cancellationToken);
}

public sealed class ArchiveFrameworkCommandHandler : IRequestHandler<ArchiveFrameworkCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public ArchiveFrameworkCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ArchiveFrameworkCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/{request.FrameworkId}/archive",
            HttpMethod.Post, null, null, cancellationToken);
}
