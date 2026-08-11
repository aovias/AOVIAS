using System.Text.Json;
using Aovias.Release.Contracts.Deployment;
using Aovias.Runtime.Agent.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aovias.Runtime.Agent.Api.Diagnostics;

/// <summary>
/// Shared deployment boundary diagnostics for Runtime Agent controllers and middleware (Sprint 11.3).
/// </summary>
public static class DeploymentBoundaryDiagnostics
{
    public static bool IsDeploymentBoundaryPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/api/deployment", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.StartsWith("/api/releases/", StringComparison.OrdinalIgnoreCase)
            && (value.Contains("/deploy", StringComparison.OrdinalIgnoreCase)
                || value.Contains("/upload", StringComparison.OrdinalIgnoreCase)
                || value.Contains("/validate", StringComparison.OrdinalIgnoreCase)
                || value.Contains("/plan", StringComparison.OrdinalIgnoreCase)
                || value.Equals("/api/releases", StringComparison.OrdinalIgnoreCase));
    }

    public static void LogControllerEntry(
        ILogger logger,
        HttpContext httpContext,
        string controllerName,
        string actionName,
        object? requestModel = null)
    {
        var correlationId = httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault()
            ?? httpContext.TraceIdentifier;

        logger.LogInformation(
            "{Controller} entered. Route={Route} Method={Method} ContentType={ContentType} RequestSize={RequestSize} CorrelationId={CorrelationId} Action={Action}",
            controllerName,
            httpContext.Request.Path.Value,
            httpContext.Request.Method,
            httpContext.Request.ContentType ?? "(none)",
            httpContext.Request.ContentLength?.ToString() ?? "(unknown)",
            correlationId,
            actionName);

        if (requestModel is not null)
        {
            logger.LogInformation(
                "{Controller} request model type={ModelType} payload={Payload}",
                controllerName,
                requestModel.GetType().Name,
                SerializeSafe(requestModel));
        }
    }

    public static void LogModelState(
        ILogger logger,
        string controllerName,
        ModelStateDictionary modelState)
    {
        if (modelState.IsValid)
        {
            logger.LogInformation("{Controller} ModelState valid.", controllerName);
            return;
        }

        foreach (var entry in modelState.Where(item => item.Value?.Errors.Count > 0))
        {
            foreach (var error in entry.Value!.Errors)
            {
                logger.LogWarning(
                    "{Controller} ModelState error Field={Field} Error={Error}",
                    controllerName,
                    entry.Key,
                    error.ErrorMessage);
            }
        }
    }

    public static IActionResult ValidationError(
        string reason,
        string recovery,
        string? field = null,
        string? endpoint = null,
        string? expectedRequestType = null,
        string phase = "Request",
        string component = "DeploymentController",
        string operation = "Validate")
        => new BadRequestObjectResult(new DeploymentFailure
        {
            Category = "RequestValidation",
            Phase = phase,
            Component = component,
            Operation = operation,
            Message = reason,
            Recovery = recovery,
            Field = field,
            Documentation = expectedRequestType ?? string.Empty
        });

    public static DeploymentFailure FromModelState(
        ModelStateDictionary modelState,
        string endpoint,
        string expectedRequestType)
    {
        var firstError = modelState
            .Where(item => item.Value?.Errors.Count > 0)
            .Select(item => new { item.Key, item.Value!.Errors[0].ErrorMessage })
            .FirstOrDefault();

        return new DeploymentFailure
        {
            Category = "RequestValidation",
            Phase = "Request",
            Component = endpoint.StartsWith("/api/releases", StringComparison.OrdinalIgnoreCase)
                ? "ReleasesController"
                : "DeploymentController",
            Operation = "ModelBinding",
            Message = firstError?.ErrorMessage ?? "Request validation failed.",
            Field = firstError?.Key,
            Recovery = "Verify the request payload matches the expected deployment contract.",
            Documentation = expectedRequestType
        };
    }

    private static string SerializeSafe(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }
}
