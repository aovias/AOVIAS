using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetHospitalFrameworksQueryHandler : IRequestHandler<GetHospitalFrameworksQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetHospitalFrameworksQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetHospitalFrameworksQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_ID_REQUIRED, traceId);
        }

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/frameworks",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
