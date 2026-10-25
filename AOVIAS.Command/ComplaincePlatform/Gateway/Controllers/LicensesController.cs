using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Api;

namespace Gateway.Controllers;

/// <summary>Phase 6 — commercial licensing surface (proxies OrganizationService internal APIs).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licenses")]
[Authorize]
public sealed class LicensesController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(IProxyService proxy, ILogger<LicensesController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    [HttpPost("issue")]
    public Task<IActionResult> Issue([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/issue", body, ct);

    [HttpPost("activate")]
    public Task<IActionResult> Activate([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/activate", body, ct);

    [HttpGet("status")]
    public Task<IActionResult> Status([FromQuery] Guid organizationId, CancellationToken ct)
        => Proxy("GET", $"/internal/licenses/status?organizationId={organizationId}", null, ct);

    [HttpPost("heartbeat")]
    public Task<IActionResult> Heartbeat([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/heartbeat", body, ct);

    [HttpPost("seats/assign")]
    public Task<IActionResult> AssignSeat([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/seats/assign", body, ct);

    [HttpGet("entitlements/check")]
    public Task<IActionResult> CheckEntitlement(
        [FromQuery] Guid organizationId,
        [FromQuery] string featureCode,
        CancellationToken ct)
        => Proxy("GET", $"/internal/licenses/entitlements/check?organizationId={organizationId}&featureCode={Uri.EscapeDataString(featureCode ?? string.Empty)}", null, ct);

    [HttpGet("entitlements")]
    public Task<IActionResult> ListEntitlements([FromQuery] Guid organizationId, CancellationToken ct)
        => Proxy("GET", $"/internal/licenses/entitlements?organizationId={organizationId}", null, ct);

    [HttpPost("entitlements/evaluate")]
    public Task<IActionResult> EvaluateEntitlements([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/entitlements/evaluate", body, ct);

    [HttpGet("summary")]
    public Task<IActionResult> Summary([FromQuery] Guid organizationId, CancellationToken ct)
        => Proxy("GET", $"/internal/licenses/summary?organizationId={organizationId}", null, ct);

    [HttpPost("catalog/backfill")]
    [Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.CommercialAdmin)]
    public Task<IActionResult> CatalogBackfill(CancellationToken ct)
        => Proxy("POST", "/internal/licenses/catalog/backfill", null, ct);

    [HttpPost("suspend")]
    [Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.CommercialAdmin)]
    public Task<IActionResult> Suspend([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/suspend", body, ct);

    [HttpPost("archive")]
    [Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.CommercialAdmin)]
    public Task<IActionResult> Archive([FromBody] object body, CancellationToken ct)
        => Proxy("POST", "/internal/licenses/archive", body, ct);

    [HttpGet("activations")]
    public Task<IActionResult> Activations([FromQuery] Guid organizationId, CancellationToken ct)
        => Proxy("GET", $"/internal/licenses/activations?organizationId={organizationId}", null, ct);

    [HttpGet("usage")]
    public Task<IActionResult> Usage([FromQuery] Guid organizationId, CancellationToken ct)
        => Proxy("GET", $"/internal/licenses/usage?organizationId={organizationId}", null, ct);

    private async Task<IActionResult> Proxy(string method, string endpoint, object? body, CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        try
        {
            var httpMethod = method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Get
                : method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Patch
                : HttpMethod.Post;

            return await _proxy.ProxyToOrganizationServiceAsync(
                endpoint: endpoint,
                httpMethod: httpMethod,
                requestBody: body,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "License proxy failed for {Endpoint}", endpoint);
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.INTERNAL_ERROR, requestId);
        }
    }
}
