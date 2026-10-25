using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetFrameworkStructureV11QueryHandler : IRequestHandler<GetFrameworkStructureV11Query, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetFrameworkStructureV11QueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetFrameworkStructureV11Query request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.FrameworkId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var query = new List<string>();
        if (request.OperationalMode.HasValue)
        {
            query.Add($"operationalMode={Uri.EscapeDataString(request.OperationalMode.Value.ToString())}");
        }

        if (!string.IsNullOrWhiteSpace(request.Classifications))
        {
            query.Add($"classifications={Uri.EscapeDataString(request.Classifications)}");
        }

        query.Add($"page={request.Page}");
        query.Add($"pageSize={request.PageSize}");

        var endpoint = $"/api/v{request.ApiVersion}/frameworks/{request.FrameworkId}/structure";
        if (query.Count > 0)
        {
            endpoint += "?" + string.Join("&", query);
        }

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
