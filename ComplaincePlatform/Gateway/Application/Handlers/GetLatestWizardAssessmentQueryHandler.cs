using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetLatestWizardAssessmentQueryHandler : IRequestHandler<GetLatestWizardAssessmentQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetLatestWizardAssessmentQueryHandler> _logger;

    public GetLatestWizardAssessmentQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetLatestWizardAssessmentQueryHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetLatestWizardAssessmentQuery request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var traceId = httpContext?.TraceIdentifier;

        if (request.HospitalId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_ID_REQUIRED, traceId);
        }

        var hospitalClaim = httpContext?.User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalClaim) || !Guid.TryParse(hospitalClaim, out var tokenHospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, traceId);
        }

        if (request.HospitalId != tokenHospitalId)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, traceId);
        }

        _logger.LogInformation(
            "GET wizard assessment latest: proxying for HospitalId={HospitalId}",
            request.HospitalId);

        return await _proxyService.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/onboarding/wizard/hospitals/{request.HospitalId}/assessments/latest",
            HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
