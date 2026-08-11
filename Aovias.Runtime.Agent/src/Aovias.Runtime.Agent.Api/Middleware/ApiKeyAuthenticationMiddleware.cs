using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Api.Diagnostics;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Api.Middleware;

/// <summary>
/// Validates <c>X-Api-Key</c> for Runtime Agent deployment APIs when configured.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate next;
    private readonly IOptionsMonitor<SecuritySettings> securitySettings;
    private readonly EnvironmentRegistrationStore registrationStore;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IOptionsMonitor<SecuritySettings> securitySettings,
        EnvironmentRegistrationStore registrationStore,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        this.next = next;
        this.securitySettings = securitySettings;
        this.registrationStore = registrationStore;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAuthentication(context.Request.Path))
        {
            await next(context);
            return;
        }

        var correlationId = context.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault()
            ?? context.TraceIdentifier;

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning(
                "ApiKey middleware rejected request. Route={Route} Method={Method} Reason=MissingApiKey CorrelationId={CorrelationId}",
                context.Request.Path.Value,
                context.Request.Method,
                correlationId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is required." });
            return;
        }

        var configuredKey = securitySettings.CurrentValue.ApiKey;
        var isValid = !string.IsNullOrWhiteSpace(configuredKey) &&
                      string.Equals(configuredKey, apiKey.ToString(), StringComparison.Ordinal);

        if (!isValid)
        {
            isValid = registrationStore.ValidateApiKey(apiKey!);
        }

        if (!isValid)
        {
            logger.LogWarning(
                "ApiKey middleware rejected request. Route={Route} Method={Method} Reason=InvalidApiKey CorrelationId={CorrelationId}",
                context.Request.Path.Value,
                context.Request.Method,
                correlationId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is invalid." });
            return;
        }

        logger.LogInformation(
            "ApiKey middleware passed. Route={Route} Method={Method} CorrelationId={CorrelationId}",
            context.Request.Path.Value,
            context.Request.Method,
            correlationId);

        await next(context);
    }

    private static bool RequiresAuthentication(PathString path)
    {
        var isDeployment = path.StartsWithSegments("/api/deployment", StringComparison.OrdinalIgnoreCase);
        var isRelease = path.StartsWithSegments("/api/releases", StringComparison.OrdinalIgnoreCase);
        if (!isDeployment && !isRelease)
        {
            return false;
        }

        if (isDeployment
            && path.StartsWithSegments("/api/deployment/register", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Allow unauthenticated deployment API ping used by publish verification.
        if (isDeployment
            && path.StartsWithSegments("/api/deployment/status", StringComparison.OrdinalIgnoreCase)
            && !path.Value!.Contains("/status/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
