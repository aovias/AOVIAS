using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public sealed class GetCoreAssessmentQueryHandler : IRequestHandler<GetCoreAssessmentQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetCoreAssessmentQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetCoreAssessmentQuery request, CancellationToken cancellationToken)
    {
        if (request.FrameworkId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/{request.FrameworkId}/core-assessment?count={request.Count}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}
