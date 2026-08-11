using System.Collections.Concurrent;
using System.Text.Json;
using Gateway.Configuration;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Api;

namespace Gateway.Middleware;

/// <summary>
/// CRP Phase 6 — fail-closed entitlement gate on mutating APIs when a route maps to a feature code.
/// Reads stay soft (optional advisory header). Auth/activate/commercial-admin allowlisted.
/// </summary>
public sealed class LicenseEntitlementMiddleware
{
    private static readonly ConcurrentDictionary<string, (bool Allowed, DateTime CachedAtUtc)> Cache = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseEntitlementMiddleware> _logger;

    public LicenseEntitlementMiddleware(RequestDelegate next, ILogger<LicenseEntitlementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<LicensingFeatureGateOptions> options)
    {
        var opts = options.Value;
        var method = context.Request.Method;
        var isMutation = !(HttpMethods.IsGet(method)
                           || HttpMethods.IsHead(method)
                           || HttpMethods.IsOptions(method));
        var path = context.Request.Path.Value ?? string.Empty;

        string? requiredFeature = null;
        if (context.Request.Headers.TryGetValue("X-Require-Entitlement", out var headerFeature)
            && !string.IsNullOrWhiteSpace(headerFeature))
        {
            requiredFeature = headerFeature.ToString().Trim();
        }
        else
        {
            requiredFeature = ResolveRouteFeature(path, opts);
        }

        if (string.IsNullOrWhiteSpace(requiredFeature))
        {
            await _next(context);
            return;
        }

        if (IsAllowlisted(path, opts))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var orgClaim = context.User.FindFirst("organization_id")?.Value
                       ?? context.User.FindFirst("organizationId")?.Value
                       ?? context.User.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgClaim, out var organizationId) || organizationId == Guid.Empty)
        {
            await _next(context);
            return;
        }

        var allowed = await CheckEntitlementAsync(
            httpClientFactory,
            organizationId,
            requiredFeature!,
            opts.EntitlementCacheSeconds,
            context.RequestAborted);

        if (!isMutation)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Entitlement-Required"] = requiredFeature!;
                context.Response.Headers["X-Entitlement-Allowed"] = allowed ? "true" : "false";
                return Task.CompletedTask;
            });
            await _next(context);
            return;
        }

        if (!opts.FailClosedWrites)
        {
            if (!allowed)
            {
                _logger.LogWarning(
                    "Entitlement denied (advisory) {Feature} for org {Org} on {Method} {Path}",
                    requiredFeature, organizationId, method, path);
            }

            await _next(context);
            return;
        }

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponseFactory.Failure<object>(
                ApiResponseCode.FORBIDDEN,
                context.TraceIdentifier));
            _logger.LogInformation(
                "Entitlement fail-closed blocked {Method} {Path} feature {Feature} org {Org}",
                method, path, requiredFeature, organizationId);
            return;
        }

        await _next(context);
    }

    private static string? ResolveRouteFeature(string path, LicensingFeatureGateOptions opts)
    {
        var p = path.ToLowerInvariant();
        foreach (var gate in opts.FeatureGates)
        {
            if (p.Contains(gate.Key.ToLowerInvariant(), StringComparison.Ordinal))
            {
                return gate.Value;
            }
        }

        return null;
    }

    private static bool IsAllowlisted(string path, LicensingFeatureGateOptions opts)
    {
        var p = path.ToLowerInvariant();
        return opts.AllowlistPathFragments.Any(f =>
            p.Contains(f.ToLowerInvariant(), StringComparison.Ordinal));
    }

    private async Task<bool> CheckEntitlementAsync(
        IHttpClientFactory httpClientFactory,
        Guid organizationId,
        string featureCode,
        int cacheSeconds,
        CancellationToken ct)
    {
        var cacheKey = $"{organizationId:N}:{featureCode}";
        var ttl = TimeSpan.FromSeconds(Math.Clamp(cacheSeconds, 5, 300));
        if (Cache.TryGetValue(cacheKey, out var cached)
            && DateTime.UtcNow - cached.CachedAtUtc < ttl)
        {
            return cached.Allowed;
        }

        try
        {
            var client = httpClientFactory.CreateClient("OrganizationClient");
            using var response = await client.GetAsync(
                $"/internal/licenses/entitlements/check?organizationId={organizationId}&featureCode={Uri.EscapeDataString(featureCode)}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                // Fail-closed on mutations relies on caller; treat upstream failure as deny for safety when checking.
                _logger.LogWarning("Entitlement check upstream failed {Status} for {Org} {Feature}",
                    response.StatusCode, organizationId, featureCode);
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d
                : root.TryGetProperty("Data", out var d2) ? d2
                : root;
            var allowed = data.TryGetProperty("allowed", out var a) && a.ValueKind is JsonValueKind.True
                          || data.TryGetProperty("Allowed", out var a2) && a2.ValueKind is JsonValueKind.True;
            Cache[cacheKey] = (allowed, DateTime.UtcNow);
            return allowed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entitlement check failed for {Org} {Feature} — deny", organizationId, featureCode);
            return false;
        }
    }
}
