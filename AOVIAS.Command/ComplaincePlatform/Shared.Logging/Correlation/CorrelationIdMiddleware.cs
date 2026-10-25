using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Logging.Correlation;

/// <summary>
/// Establishes an end-to-end correlation id for every request and pushes
/// correlation / identity properties into the Serilog <see cref="LogContext"/>
/// so they appear on every log event produced while handling the request.
///
/// Order: register this BEFORE request logging so the request-completed event is enriched.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[LoggingConstants.CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[LoggingConstants.CorrelationItemKey] = correlationId;

        // Emit the correlation id on the response (set before the body is written).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[LoggingConstants.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString();
        var spanId = activity?.SpanId.ToString();

        using (LogContext.PushProperty(LoggingConstants.Properties.CorrelationId, correlationId))
        using (LogContext.PushProperty(LoggingConstants.Properties.RequestId, context.TraceIdentifier))
        using (PushIfPresent(LoggingConstants.Properties.TraceId, traceId))
        using (PushIfPresent(LoggingConstants.Properties.SpanId, spanId))
        using (PushIfPresent(LoggingConstants.Properties.UserId, GetClaim(context, "sub", ClaimTypes.NameIdentifier)))
        using (PushIfPresent(LoggingConstants.Properties.OrganizationId, GetClaim(context, "organization_id")))
        using (PushIfPresent(LoggingConstants.Properties.HospitalId, GetClaim(context, "hospital_id")))
        {
            await _next(context);
        }
    }

    private static IDisposable PushIfPresent(string name, string? value)
        => string.IsNullOrWhiteSpace(value)
            ? NullScope.Instance
            : LogContext.PushProperty(name, value);

    private static string? GetClaim(HttpContext context, params string[] claimTypes)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = context.User.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
