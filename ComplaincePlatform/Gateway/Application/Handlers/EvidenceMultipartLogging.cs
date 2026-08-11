using Microsoft.AspNetCore.Http;

namespace Gateway.Application.Handlers;

internal static class EvidenceMultipartLogging
{
    public static string? GetCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var a) && a.Count > 0)
        {
            return a[0];
        }

        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var b) && b.Count > 0)
        {
            return b[0];
        }

        return null;
    }
}
