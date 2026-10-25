using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Infrastructure.Execution;

/// <summary>Reads the execution context for the current unit of work.</summary>
public interface IExecutionContextAccessor
{
    /// <summary>The current context, or <c>null</c> when none has been established.</summary>
    PlatformExecutionContext? Current { get; }

    /// <summary>
    /// The tenant boundary, or <see cref="Guid.Empty"/> when no valid context exists.
    ///
    /// <para>
    /// Returning <see cref="Guid.Empty"/> rather than throwing is deliberate: query filters compare
    /// against it and match nothing, so the platform **fails closed**. A caller that wants a hard
    /// failure should check <see cref="PlatformExecutionContext.IsValid"/> explicitly.
    /// </para>
    /// </summary>
    Guid OrganizationId { get; }
}

/// <summary>
/// Resolves the execution context from the current HTTP request.
///
/// <para>
/// Order of precedence:
/// 1. A context already materialised onto <c>HttpContext.Items</c> by middleware.
/// 2. Propagated <see cref="ExecutionContextHeaders"/> — internal service-to-service calls.
/// 3. Validated token claims — direct user requests.
/// </para>
///
/// <para>
/// Header values are trusted only because the edge strips them from public traffic; see
/// <see cref="ExecutionContextHeaders"/>.
/// </para>
/// </summary>
public sealed class ExecutionContextAccessor : IExecutionContextAccessor
{
    /// <summary>Key under which middleware caches the materialised context.</summary>
    public const string HttpContextItemKey = "__aovias_execution_context";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExecutionContextAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public PlatformExecutionContext? Current
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            if (http is null) return null;

            if (http.Items.TryGetValue(HttpContextItemKey, out var cached)
                && cached is PlatformExecutionContext ctx)
            {
                return ctx;
            }

            var resolved = Resolve(http);
            if (resolved is not null)
            {
                http.Items[HttpContextItemKey] = resolved;
            }

            return resolved;
        }
    }

    public Guid OrganizationId => Current is { IsValid: true } c ? c.OrganizationId : Guid.Empty;

    private static PlatformExecutionContext? Resolve(HttpContext http)
    {
        var correlationId = FirstNonEmpty(
            http.Request.Headers[ExecutionContextHeaders.CorrelationId].FirstOrDefault(),
            http.TraceIdentifier,
            Guid.NewGuid().ToString("N"));

        // (2) Propagated context — an internal hop.
        var headerOrg = ParseGuid(http.Request.Headers[ExecutionContextHeaders.OrganizationId].FirstOrDefault());
        if (headerOrg != Guid.Empty)
        {
            return new PlatformExecutionContext
            {
                OrganizationId = headerOrg,
                HospitalId = ParseNullableGuid(http.Request.Headers[ExecutionContextHeaders.HospitalId].FirstOrDefault()),
                JourneyId = ParseNullableGuid(http.Request.Headers[ExecutionContextHeaders.JourneyId].FirstOrDefault()),
                FrameworkId = ParseNullableGuid(http.Request.Headers[ExecutionContextHeaders.FrameworkId].FirstOrDefault()),
                UserId = ParseNullableGuid(http.Request.Headers[ExecutionContextHeaders.UserId].FirstOrDefault()),
                Mode = ParseMode(http.Request.Headers[ExecutionContextHeaders.ExecutionMode].FirstOrDefault()),
                CorrelationId = correlationId,
                RequestId = FirstNonEmpty(
                    http.Request.Headers[ExecutionContextHeaders.RequestId].FirstOrDefault(),
                    Guid.NewGuid().ToString("N")),
                Source = http.Request.Headers[ExecutionContextHeaders.Source].FirstOrDefault() ?? string.Empty,
                Caller = http.Request.Headers[ExecutionContextHeaders.Caller].FirstOrDefault() ?? string.Empty,
            };
        }

        // (3) Direct user request — claims are authoritative.
        var user = http.User;
        if (user?.Identity?.IsAuthenticated != true) return null;

        var claimOrg = ParseGuid(
            user.FindFirst("organization_id")?.Value
            ?? user.FindFirst("organizationId")?.Value
            ?? user.FindFirst("org_id")?.Value);

        if (claimOrg == Guid.Empty) return null;

        return new PlatformExecutionContext
        {
            OrganizationId = claimOrg,
            HospitalId = ParseNullableGuid(user.FindFirst("hospital_id")?.Value),
            UserId = ParseNullableGuid(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value),
            Mode = ExecutionMode.User,
            CorrelationId = correlationId,
            RequestId = Guid.NewGuid().ToString("N"),
            Source = "Gateway",
            Caller = user.FindFirst("email")?.Value ?? user.Identity?.Name ?? string.Empty,
        };
    }

    private static ExecutionMode ParseMode(string? raw) =>
        Enum.TryParse<ExecutionMode>(raw, ignoreCase: true, out var mode) ? mode : ExecutionMode.InternalApi;

    private static Guid ParseGuid(string? raw) => Guid.TryParse(raw, out var id) ? id : Guid.Empty;

    private static Guid? ParseNullableGuid(string? raw)
    {
        var id = ParseGuid(raw);
        return id == Guid.Empty ? null : id;
    }

    private static string FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? string.Empty;
}
