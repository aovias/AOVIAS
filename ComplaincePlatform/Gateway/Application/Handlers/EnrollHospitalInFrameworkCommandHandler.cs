using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class EnrollHospitalInFrameworkCommandHandler : IRequestHandler<EnrollHospitalInFrameworkCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnrollHospitalInFrameworkCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(EnrollHospitalInFrameworkCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/frameworks/{request.FrameworkId}/enroll",
            httpMethod: HttpMethod.Post,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
