using System.Net;
using System.Text.Json;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Api.Diagnostics;
using Aovias.Runtime.Agent.Api.Middleware;
using Aovias.Runtime.Agent.Infrastructure.Deployment;

namespace Aovias.Runtime.Agent.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into structured API error responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault()
                ?? context.TraceIdentifier;

            logger.LogError(
                exception,
                "Exception middleware caught unhandled exception. Method={Method} Path={Path} CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                correlationId);

            var uploadRejection = exception as UploadRejectedException;
            var statusCode = uploadRejection?.StatusCode ?? (int)HttpStatusCode.InternalServerError;
            var failure = DeploymentFailureMapper.FromException(
                exception,
                phase: uploadRejection is null ? "Request" : "Upload",
                component: ResolveComponent(context.Request.Path),
                operation: $"{context.Request.Method} {context.Request.Path.Value}",
                category: uploadRejection is null
                    ? DeploymentFailureMapper.Classify(exception)
                    : DeploymentFailureCategory.Upload,
                correlationId: correlationId,
                httpStatusCode: statusCode);

            if (uploadRejection is not null)
            {
                failure = new DeploymentFailure
                {
                    Category = failure.Category,
                    Phase = failure.Phase,
                    Component = failure.Component,
                    Operation = failure.Operation,
                    ExceptionType = failure.ExceptionType,
                    Message = failure.Message,
                    InnerExceptionMessage = failure.InnerExceptionMessage,
                    Recovery = uploadRejection.Recovery,
                    Documentation = failure.Documentation,
                    CorrelationId = failure.CorrelationId,
                    DeploymentId = failure.DeploymentId,
                    ServiceId = failure.ServiceId,
                    Path = failure.Path,
                    Field = failure.Field,
                    HttpStatusCode = statusCode,
                    StackTrace = failure.StackTrace,
                    ExceptionChain = failure.ExceptionChain,
                    IsTransient = false
                };
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(failure);
        }
    }

    private static string ResolveComponent(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/api/deployment", StringComparison.OrdinalIgnoreCase))
        {
            return "DeploymentController";
        }

        if (value.StartsWith("/api/releases", StringComparison.OrdinalIgnoreCase))
        {
            return "ReleasesController";
        }

        return "RuntimeAgent";
    }
}
