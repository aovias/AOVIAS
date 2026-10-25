using Microsoft.AspNetCore.Http;

namespace ComplianceService.Application.Http;

/// <summary>Reads correlation id from inbound request headers (Gateway / middleware convention).</summary>
public static class CorrelationIdAccessor
{
    public static string? FromHttpContext(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var primary) && primary.Count > 0)
        {
            return primary[0];
        }

        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var secondary) && secondary.Count > 0)
        {
            return secondary[0];
        }

        return null;
    }
}
