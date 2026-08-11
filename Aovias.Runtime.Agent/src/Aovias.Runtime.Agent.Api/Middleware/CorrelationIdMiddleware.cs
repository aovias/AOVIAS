namespace Aovias.Runtime.Agent.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation identifier for distributed tracing.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// Header name used for correlation identifiers.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the middleware execution.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
