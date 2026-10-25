using Microsoft.AspNetCore.Http;

namespace Shared.Logging.Correlation;

/// <summary>
/// Helpers for reading the current request's correlation id from anywhere in the pipeline.
/// </summary>
public static class CorrelationContext
{
    /// <summary>
    /// Returns the correlation id stored on the context by <see cref="CorrelationIdMiddleware"/>,
    /// falling back to the incoming header or the trace identifier.
    /// </summary>
    public static string? GetCorrelationId(HttpContext? context)
    {
        if (context is null)
        {
            return null;
        }

        if (context.Items.TryGetValue(LoggingConstants.CorrelationItemKey, out var value) && value is string existing)
        {
            return existing;
        }

        var header = context.Request.Headers[LoggingConstants.CorrelationIdHeader].FirstOrDefault();
        return string.IsNullOrWhiteSpace(header) ? context.TraceIdentifier : header;
    }
}
