using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for LoginQuery that proxies to IdentityService.
/// </summary>
public class LoginQueryHandler : IRequestHandler<LoginQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<LoginQueryHandler> _logger;

    public LoginQueryHandler(
        IProxyService proxyService,
        ILogger<LoginQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing login request");
        
        return await _proxyService.ProxyToIdentityServiceAsync(
            endpoint: "/api/v1/auth/login",
            httpMethod: HttpMethod.Post,
            requestBody: request.RequestBody,
            cancellationToken: cancellationToken);
    }
}
