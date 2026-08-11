using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record IdentityProxyQuery(string Endpoint, HttpMethod Method, object? Body) : IRequest<IActionResult>;

public record IdentityProxyCommand(string Endpoint, HttpMethod Method, object? Body) : IRequest<IActionResult>;

public class IdentityProxyQueryHandler :
    IRequestHandler<IdentityProxyQuery, IActionResult>,
    IRequestHandler<IdentityProxyCommand, IActionResult>
{
    private readonly IProxyService _proxyService;

    public IdentityProxyQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(IdentityProxyQuery request, CancellationToken cancellationToken)
        => _proxyService.ProxyToIdentityServiceAsync(request.Endpoint, request.Method, request.Body, cancellationToken);

    public Task<IActionResult> Handle(IdentityProxyCommand request, CancellationToken cancellationToken)
        => _proxyService.ProxyToIdentityServiceAsync(request.Endpoint, request.Method, request.Body, cancellationToken);
}
