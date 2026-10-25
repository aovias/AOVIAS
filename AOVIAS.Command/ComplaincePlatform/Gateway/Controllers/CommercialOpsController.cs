using System.Reflection;
using System.Text.Json;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

/// <summary>
/// Phase 6 Sprint 5 — commercial operations façade.
/// Provisions trial licenses via OrganizationService internal licensing APIs
/// and returns customer-health / diagnostics packs for platform admins.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commercial")]
[Authorize]
public sealed class CommercialOpsController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommercialOpsController> _logger;

    public CommercialOpsController(
        IProxyService proxy,
        IWebHostEnvironment env,
        IConfiguration configuration,
        ILogger<CommercialOpsController> logger)
    {
        _proxy = proxy;
        _env = env;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Issue a trial license and return an onboarding checklist.</summary>
    [HttpPost("provision")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<ProvisionTrialResponseDto>>> ProvisionTrial(
        [FromBody] ProvisionTrialRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.OrganizationId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<ProvisionTrialResponseDto>(
                ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var issueBody = new
        {
            organizationId = request.OrganizationId,
            subscriptionPlanId = (Guid?)null,
            licenseType = "Trial",
            maxSeats = request.MaxSeats <= 0 ? 10 : request.MaxSeats,
            validDays = request.ValidDays <= 0 ? 30 : request.ValidDays,
            gracePeriodDays = request.GracePeriodDays,
            entitlements = request.Entitlements,
            customerName = request.CustomerName
        };

        try
        {
            var proxied = await _proxy.ProxyToOrganizationServiceAsync(
                "/internal/licenses/issue",
                HttpMethod.Post,
                issueBody,
                cancellationToken);

            var (ok, payload) = await ExtractJsonAsync(proxied, cancellationToken);
            if (!ok || payload is null)
            {
                _logger.LogWarning("Trial provision proxy failed for org {Org}", request.OrganizationId);
                return ApiResponseFactory.FailureResult<ProvisionTrialResponseDto>(
                    ApiResponseCode.INTERNAL_ERROR, traceId);
            }

            var dataEl = payload.Value.TryGetProperty("data", out var d) ? d : payload.Value;
            var keyCode = dataEl.TryGetProperty("keyCode", out var kc) ? kc.GetString() : null;
            Guid? keyId = dataEl.TryGetProperty("id", out var idEl) && idEl.TryGetGuid(out var g) ? g : null;
            DateTime? expires = null;
            if (dataEl.TryGetProperty("expiresAtUtc", out var exp) && exp.ValueKind == JsonValueKind.String
                && DateTime.TryParse(exp.GetString(), out var parsed))
            {
                expires = parsed.ToUniversalTime();
            }

            var maxSeats = dataEl.TryGetProperty("maxSeats", out var ms) && ms.TryGetInt32(out var msi)
                ? msi
                : request.MaxSeats;

            var checklist = new List<string>
            {
                "Activate the license key in Settings → Platform → Licensing (or POST /api/v1/licenses/activate).",
                "Invite the hospital admin and assign seats via POST /api/v1/licenses/seats/assign.",
                "Complete onboarding wizard (POST /api/v1/onboarding/onboard) for frameworks and assessment.",
                "Verify entitlements with GET /api/v1/licenses/entitlements/check.",
                "Confirm heartbeat from the deployed host (POST /api/v1/licenses/heartbeat).",
                "Review customer health: GET /api/v1/commercial/customer-health?organizationId="
            };

            var dto = new ProvisionTrialResponseDto
            {
                OrganizationId = request.OrganizationId,
                LicenseKeyCode = keyCode,
                LicenseKeyId = keyId,
                ExpiresAtUtc = expires,
                MaxSeats = maxSeats,
                Checklist = checklist,
                Message = "Trial license issued. Complete the checklist to finish commercial bootstrap."
            };

            var apiResponse = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, apiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Commercial provision failed");
            return ApiResponseFactory.FailureResult<ProvisionTrialResponseDto>(
                ApiResponseCode.INTERNAL_ERROR, traceId);
        }
    }

    /// <summary>License status plus readiness stubs for support triage.</summary>
    [HttpGet("customer-health")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<CustomerHealthDto>>> CustomerHealth(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (organizationId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<CustomerHealthDto>(
                ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        object? licenseStatus = null;
        try
        {
            var proxied = await _proxy.ProxyToOrganizationServiceAsync(
                $"/internal/licenses/status?organizationId={organizationId}",
                HttpMethod.Get,
                null,
                cancellationToken);
            var (ok, payload) = await ExtractJsonAsync(proxied, cancellationToken);
            if (ok && payload is not null)
            {
                licenseStatus = payload.Value.TryGetProperty("data", out var d)
                    ? JsonSerializer.Deserialize<object>(d.GetRawText())
                    : JsonSerializer.Deserialize<object>(payload.Value.GetRawText());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "License status unavailable for {Org}", organizationId);
        }

        var readiness = new List<ReadinessStub>
        {
            new() { Area = "license", Status = licenseStatus is null ? "unavailable" : "ok", Detail = "License status from OrganizationService" },
            new() { Area = "onboarding", Status = "stub", Detail = "Check GET /api/v1/onboarding status for org/hospital completion" },
            new() { Area = "identity", Status = "stub", Detail = "Verify admin user can authenticate against IdentityService" },
            new() { Area = "compliance", Status = "stub", Detail = "Verify at least one compliance journey exists" },
            new() { Area = "backup", Status = System.IO.File.Exists(Path.Combine(_env.ContentRootPath, "App_Data", "backups", "manifest.json")) ? "ok" : "missing", Detail = "App_Data/backups/manifest.json" }
        };

        var dto = new CustomerHealthDto
        {
            OrganizationId = organizationId,
            LicenseStatus = licenseStatus,
            Readiness = readiness
        };

        var apiResponse = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, apiResponse);
    }

    /// <summary>Redacted diagnostics pack: versions, license summary, health stubs.</summary>
    [HttpGet("diagnostics")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<IActionResult> DiagnosticsPack(
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        object? license = null;
        if (organizationId is { } orgId && orgId != Guid.Empty)
        {
            try
            {
                var proxied = await _proxy.ProxyToOrganizationServiceAsync(
                    $"/internal/licenses/status?organizationId={orgId}",
                    HttpMethod.Get,
                    null,
                    cancellationToken);
                var (ok, payload) = await ExtractJsonAsync(proxied, cancellationToken);
                if (ok && payload is not null && payload.Value.TryGetProperty("data", out var d))
                {
                    // Redact key code partially
                    var status = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(d.GetRawText())
                                 ?? new Dictionary<string, JsonElement>();
                    if (status.TryGetValue("keyCode", out var keyEl))
                    {
                        var raw = keyEl.GetString() ?? "";
                        status["keyCode"] = JsonSerializer.SerializeToElement(Redact(raw));
                    }
                    license = status;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Diagnostics license fetch failed");
            }
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var versionFile = Path.Combine(_env.ContentRootPath, "..", "VERSION");
        if (System.IO.File.Exists(versionFile))
        {
            version = (await System.IO.File.ReadAllTextAsync(versionFile, cancellationToken)).Trim();
        }

        var pack = new
        {
            generatedAtUtc = DateTime.UtcNow,
            environment = _env.EnvironmentName,
            gatewayVersion = version,
            organizationId = organizationId,
            license = license,
            config = new
            {
                sqlConfigured = !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection")),
                identityAuthoritySet = !string.IsNullOrWhiteSpace(_configuration["Authentication:Authority"]),
                // Never include secrets
                secretsRedacted = true
            },
            health = new
            {
                gateway = "ok",
                note = "Downstream probes are stubs in Phase 6; expand with IProxyService health checks later."
            },
            backupManifestPresent = System.IO.File.Exists(
                Path.Combine(_env.ContentRootPath, "App_Data", "backups", "manifest.json"))
        };

        var json = JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"aovias-diagnostics-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
    }

    private static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 8) return "***";
        return value[..4] + "…" + value[^4..];
    }

    private static async Task<(bool Ok, JsonElement? Payload)> ExtractJsonAsync(
        IActionResult result,
        CancellationToken cancellationToken)
    {
        if (result is ObjectResult { Value: not null } obj)
        {
            var text = JsonSerializer.Serialize(obj.Value);
            using var doc = JsonDocument.Parse(text);
            return (obj.StatusCode is null or >= 200 and < 300, doc.RootElement.Clone());
        }

        if (result is ContentResult { Content: not null } content)
        {
            using var doc = JsonDocument.Parse(content.Content);
            var ok = content.StatusCode is null or >= 200 and < 300;
            return (ok, doc.RootElement.Clone());
        }

        // Some proxy implementations return StatusCodeResult / empty — treat as failure
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        return (false, null);
    }
}
