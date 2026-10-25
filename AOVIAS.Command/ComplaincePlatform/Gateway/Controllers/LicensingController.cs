using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Controllers;

/// <summary>CRP Phase 6 — unified licensing read façade (LicenseKey SSOT + catalogue enrichment).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licensing")]
[Authorize]
public sealed class LicensingController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly ILogger<LicensingController> _logger;

    public LicensingController(IProxyService proxy, ILogger<LicensingController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        try
        {
            return await _proxy.ProxyToOrganizationServiceAsync(
                endpoint: $"/internal/licenses/summary?organizationId={organizationId}",
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Licensing summary failed");
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.INTERNAL_ERROR, requestId);
        }
    }
}
