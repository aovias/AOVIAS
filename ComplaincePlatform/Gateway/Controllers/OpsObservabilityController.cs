using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Caching;

namespace Gateway.Controllers;

/// <summary>Phase 7.4 — ops observability summary (dependency health stubs).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ops/observability")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class OpsObservabilityController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public OpsObservabilityController(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    /// <summary>
    /// Returns a lightweight observability summary: Redis/OTLP/App Insights config presence
    /// and stub dependency health notes (not live probes).
    /// </summary>
    [HttpGet("summary")]
    public ActionResult<object> Summary()
    {
        var traceId = HttpContext.TraceIdentifier;
        var redisConfigured = RedisConfiguration.IsConfigured(_configuration);
        var otlp = _configuration["OpenTelemetry:OtlpEndpoint"];
        var appInsights =
            _configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? _configuration["ApplicationInsights:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        var data = new
        {
            service = "Gateway",
            environment = _env.EnvironmentName,
            telemetry = new
            {
                otlpConfigured = !string.IsNullOrWhiteSpace(otlp),
                otlpEndpoint = string.IsNullOrWhiteSpace(otlp) ? null : otlp,
                azureMonitorConfigured = !string.IsNullOrWhiteSpace(appInsights),
                note = "Exporters are no-op when endpoints/connection strings are unset."
            },
            dependencies = new[]
            {
                new
                {
                    name = "Redis",
                    configured = redisConfigured,
                    status = redisConfigured ? "configured" : "not_configured",
                    note = "Stub — set ConnectionStrings:Redis or Redis:Configuration for distributed cache + SignalR backplane."
                },
                new
                {
                    name = "SqlServer",
                    configured = !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection")),
                    status = "stub",
                    note = "Use /health and /health/ready for live probes; this summary does not open a SQL connection."
                },
                new
                {
                    name = "IdentityService",
                    configured = !string.IsNullOrWhiteSpace(_configuration["Services:IdentityService:BaseUrl"]),
                    status = "stub",
                    note = "Downstream base URL present in config; not probed here."
                },
                new
                {
                    name = "ComplianceService",
                    configured = !string.IsNullOrWhiteSpace(_configuration["Services:ComplianceService:BaseUrl"]),
                    status = "stub",
                    note = "Downstream base URL present in config; not probed here."
                }
            },
            alerting = new
            {
                runbook = "docs/commercial-readiness/phase-7/04-observability.md",
                note = "Wire Azure Monitor / OTLP alerts per runbook; this endpoint is config inventory only."
            },
            generatedAtUtc = DateTimeOffset.UtcNow
        };

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }
}
