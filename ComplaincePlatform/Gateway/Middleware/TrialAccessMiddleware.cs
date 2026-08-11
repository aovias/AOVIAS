using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Api;

namespace Gateway.Middleware;

/// <summary>CRP Phase 5 — block mutating tenant APIs when license is Expired/Revoked (read-only 1A).</summary>
public sealed class TrialAccessMiddleware
{
    private static readonly ConcurrentDictionary<Guid, (string Status, DateTime CachedAtUtc)> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);

    private readonly RequestDelegate _next;
    private readonly ILogger<TrialAccessMiddleware> _logger;

    public TrialAccessMiddleware(RequestDelegate next, ILogger<TrialAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<Configuration.TrialLifecycleGatewayOptions> options)
    {
        var opts = options.Value;
        if (!opts.EnforceReadOnly)
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            await AttachTrialHeaderAndContinue(context, httpClientFactory);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsAllowlisted(path))
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

        var status = await ResolveStatusAsync(httpClientFactory, organizationId, context.RequestAborted);
        if (status is "Expired" or "Revoked" or "Suspended" or "Archived")
        {
            context.Response.Headers["X-Trial-Status"] = status;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponseFactory.Failure<object>(
                ApiResponseCode.FORBIDDEN,
                context.TraceIdentifier));
            _logger.LogInformation("Read-only gate blocked {Method} {Path} org {Org} status {Status}",
                method, path, organizationId, status);
            return;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Trial-Status"] = status!;
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private async Task AttachTrialHeaderAndContinue(HttpContext context, IHttpClientFactory httpClientFactory)
    {
        try
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var orgClaim = context.User.FindFirst("organization_id")?.Value
                               ?? context.User.FindFirst("organizationId")?.Value;
                if (Guid.TryParse(orgClaim, out var organizationId) && organizationId != Guid.Empty)
                {
                    var status = await ResolveStatusAsync(httpClientFactory, organizationId, context.RequestAborted);
                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        context.Response.OnStarting(() =>
                        {
                            context.Response.Headers["X-Trial-Status"] = status!;
                            return Task.CompletedTask;
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Trial header attach skipped");
        }

        await _next(context);
    }

    private static bool IsAllowlisted(string path)
    {
        var p = path.ToLowerInvariant();
        return p.Contains("/commercial-admin", StringComparison.Ordinal)
               || p.Contains("/licenses/status", StringComparison.Ordinal)
               || p.Contains("/licenses/activate", StringComparison.Ordinal)
               || p.Contains("/identity/auth", StringComparison.Ordinal)
               || p.Contains("/auth/", StringComparison.Ordinal)
               || p.Contains("/health", StringComparison.Ordinal)
               || p.Contains("/ops/", StringComparison.Ordinal)
               || p.Contains("password", StringComparison.Ordinal)
               || p.Contains("refresh", StringComparison.Ordinal);
    }

    private async Task<string?> ResolveStatusAsync(
        IHttpClientFactory httpClientFactory,
        Guid organizationId,
        CancellationToken ct)
    {
        if (Cache.TryGetValue(organizationId, out var cached)
            && DateTime.UtcNow - cached.CachedAtUtc < CacheTtl)
        {
            return cached.Status;
        }

        try
        {
            var client = httpClientFactory.CreateClient("OrganizationClient");
            using var response = await client.GetAsync(
                $"/internal/licenses/status?organizationId={organizationId}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d
                : root.TryGetProperty("Data", out var d2) ? d2
                : root;
            var status = data.TryGetProperty("status", out var s) ? s.GetString()
                : data.TryGetProperty("Status", out var s2) ? s2.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                Cache[organizationId] = (status, DateTime.UtcNow);
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Trial status lookup failed (fail-open) for {Org}", organizationId);
            return null;
        }
    }
}
