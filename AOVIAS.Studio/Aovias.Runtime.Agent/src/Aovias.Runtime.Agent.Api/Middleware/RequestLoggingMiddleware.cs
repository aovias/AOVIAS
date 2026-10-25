namespace Aovias.Runtime.Agent.Api.Middleware;

/// <summary>
/// Logs inbound HTTP requests and outbound responses.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLoggingMiddleware> logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = DateTimeOffset.UtcNow;
        var isDeploymentBoundary = Diagnostics.DeploymentBoundaryDiagnostics.IsDeploymentBoundaryPath(context.Request.Path);
        var correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault()
            ?? context.TraceIdentifier;

        if (isDeploymentBoundary)
        {
            logger.LogInformation(
                "Deployment boundary request started. Method={Method} Route={Path} ContentType={ContentType} RequestSize={RequestSize} CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.ContentType ?? "(none)",
                context.Request.ContentLength?.ToString() ?? "(unknown)",
                correlationId);
        }
        else
        {
            logger.LogInformation(
                "HTTP {Method} {Path} started",
                context.Request.Method,
                context.Request.Path);
        }

        await next(context);

        var elapsed = DateTimeOffset.UtcNow - started;
        if (isDeploymentBoundary)
        {
            logger.LogInformation(
                "Deployment boundary request completed. Method={Method} Route={Path} StatusCode={StatusCode} ElapsedMs={ElapsedMilliseconds} CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds,
                correlationId);

            if (context.Response.StatusCode == StatusCodes.Status400BadRequest)
            {
                logger.LogWarning(
                    "Deployment boundary returned 400. Route={Path} CorrelationId={CorrelationId}",
                    context.Request.Path.Value,
                    correlationId);
            }
        }
        else
        {
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds);
        }
    }
}
