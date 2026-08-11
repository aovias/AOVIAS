using Aovias.Runtime.Agent.Api.Diagnostics;
using Aovias.Runtime.Agent.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aovias.Runtime.Agent.Api.Filters;

/// <summary>
/// Logs deployment boundary entry, model binding, and validation for deployment controllers (Sprint 11.3).
/// </summary>
public sealed class DeploymentBoundaryDiagnosticsFilter : IActionFilter
{
    private readonly ILogger<DeploymentBoundaryDiagnosticsFilter> logger;

    public DeploymentBoundaryDiagnosticsFilter(ILogger<DeploymentBoundaryDiagnosticsFilter> logger)
    {
        this.logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!DeploymentBoundaryDiagnostics.IsDeploymentBoundaryPath(context.HttpContext.Request.Path))
        {
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "UnknownController";
        var actionName = context.RouteData.Values["action"]?.ToString() ?? "UnknownAction";
        var requestModel = context.ActionArguments.Values.FirstOrDefault();

        DeploymentBoundaryDiagnostics.LogControllerEntry(
            logger,
            context.HttpContext,
            controllerName,
            actionName,
            requestModel);

        DeploymentBoundaryDiagnostics.LogModelState(logger, controllerName, context.ModelState);

        if (!context.ModelState.IsValid)
        {
            var endpoint = context.HttpContext.Request.Path.Value ?? string.Empty;
            var expectedType = InferExpectedRequestType(context);
            context.Result = new BadRequestObjectResult(
                DeploymentBoundaryDiagnostics.FromModelState(context.ModelState, endpoint, expectedType));
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (!DeploymentBoundaryDiagnostics.IsDeploymentBoundaryPath(context.HttpContext.Request.Path))
        {
            return;
        }

        var correlationId = context.HttpContext.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault()
            ?? context.HttpContext.TraceIdentifier;

        logger.LogInformation(
            "Deployment boundary completed. Route={Route} StatusCode={StatusCode} CorrelationId={CorrelationId}",
            context.HttpContext.Request.Path.Value,
            context.HttpContext.Response.StatusCode,
            correlationId);
    }

    private static string InferExpectedRequestType(ActionExecutingContext context)
    {
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        return action switch
        {
            "DeployAsync" when context.HttpContext.Request.Path.Value?.Contains("/api/releases/", StringComparison.OrdinalIgnoreCase) == true
                => nameof(Aovias.Release.Contracts.Releases.DeployReleaseRequest),
            "DeployAsync" or "UploadAsync"
                => "multipart/form-data with package file",
            "DeployPlatformAsync" => nameof(Aovias.Release.Contracts.RemoteDeployment.PlatformDeploymentRequest),
            "ValidateAsync" => "ValidateReleaseRequest",
            "PlanAsync" => nameof(Aovias.Release.Contracts.Releases.PlanDeploymentRequest),
            _ => "deployment request DTO"
        };
    }
}
