using System.Text.Json;
using Gateway.Security;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace Gateway.Middleware;

/// <summary>
/// Rejects authenticated requests whose query/header/route org or hospital identifiers
/// conflict with JWT <c>organization_id</c> / <c>hospital_id</c> claims.
/// SUPER_ADMIN bypasses isolation (platform operators may cross tenants).
/// </summary>
public sealed class TenantIsolationMiddleware
{
    private static readonly string[] HeaderNames =
    [
        "X-Organization-Id",
        "X-Org-Id",
        "X-Hospital-Id"
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (user.IsInRole(ApplicationRoles.SUPER_ADMIN))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase) &&
            path.Contains("/setup", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        Guid? jwtOrg = ParseClaim(user, "organization_id") ?? ParseClaim(user, "org_id");
        Guid? jwtHospital = ParseClaim(user, "hospital_id");

        if (jwtOrg is null && jwtHospital is null)
        {
            await _next(context);
            return;
        }

        var candidates = CollectCandidates(context);
        var conflict = TenantIsolationEvaluator.FindConflict(jwtOrg, jwtHospital, candidates);
        if (conflict is null)
        {
            await _next(context);
            return;
        }

        _logger.LogWarning(
            "Tenant isolation conflict: source={Source} name={Name} requested={Requested} claim={ClaimType}:{ClaimValue} path={Path}",
            conflict.Source,
            conflict.Name,
            conflict.RequestedValue,
            conflict.ClaimType,
            conflict.ClaimValue,
            path);

        var traceId = context.TraceIdentifier;
        var result = ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, traceId);
        context.Response.StatusCode = result.StatusCode ?? StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(result.Value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static Guid? ParseClaim(System.Security.Claims.ClaimsPrincipal user, string type)
    {
        var value = user.FindFirst(type)?.Value;
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    }

    private static List<TenantIsolationEvaluator.Candidate> CollectCandidates(HttpContext context)
    {
        var list = new List<TenantIsolationEvaluator.Candidate>();

        foreach (var (key, value) in context.Request.Query)
        {
            if (!string.IsNullOrWhiteSpace(value))
                list.Add(new TenantIsolationEvaluator.Candidate("query", key, value.ToString()));
        }

        foreach (var headerName in HeaderNames)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerValue) &&
                !string.IsNullOrWhiteSpace(headerValue))
            {
                list.Add(new TenantIsolationEvaluator.Candidate("header", headerName, headerValue.ToString()));
            }
        }

        foreach (var (key, value) in context.Request.RouteValues)
        {
            if (value is null) continue;
            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text)) continue;
            list.Add(new TenantIsolationEvaluator.Candidate("route", key, text));
        }

        return list;
    }
}
