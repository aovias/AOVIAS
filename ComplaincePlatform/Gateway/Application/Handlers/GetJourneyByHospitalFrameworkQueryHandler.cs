using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetJourneyByHospitalFrameworkQueryHandler : IRequestHandler<GetJourneyByHospitalFrameworkQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetJourneyByHospitalFrameworkQueryHandler> _logger;

    public GetJourneyByHospitalFrameworkQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetJourneyByHospitalFrameworkQueryHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetJourneyByHospitalFrameworkQuery request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var traceId = httpContext?.TraceIdentifier;

        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var hospitalIdClaim = httpContext?.User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var tokenHospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, traceId);
        }

        if (request.HospitalId != tokenHospitalId)
        {
            _logger.LogWarning(
                "Hospital ID mismatch: requested {RequestedHospitalId} does not match token {TokenHospitalId}",
                request.HospitalId, tokenHospitalId);
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, traceId);
        }

        _logger.LogInformation(
            "GET journey by hospital/framework — proxying to ComplianceService (v{Version})",
            request.ApiVersion);

        var endpoint =
            $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/frameworks/{request.FrameworkId}/journey";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
