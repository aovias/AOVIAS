using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Logging.Health;

namespace Shared.Logging.Platform;

/// <summary>
/// Integrates PRAMAN microservices with the AOVIAS Platform (health, registration, observability hooks).
/// </summary>
public static class PramanAoviasPlatformExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Registers PRAMAN platform integration services.
    /// </summary>
    public static IServiceCollection AddPramanAoviasPlatform(
        this IServiceCollection services,
        PramanServiceRegistration registration)
    {
        services.AddSingleton(registration);
        return services;
    }

    /// <summary>
    /// Maps AOVIAS-standard health probes and the runtime registration endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapPramanAoviasPlatform(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPramanHealthChecks();

        endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = PramanHealthExtensions.WriteHealthResponse
        });

        endpoints.MapHealthChecks("/readiness", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = PramanHealthExtensions.WriteHealthResponse
        });

        endpoints.MapGet("/platform/registration", async (HttpContext context, PramanServiceRegistration registration) =>
        {
            context.Response.ContentType = "application/json";
            var payload = new
            {
                applicationId = registration.ApplicationId,
                serviceId = registration.ServiceId,
                serviceName = registration.ServiceName,
                version = registration.Version,
                healthEndpoint = registration.HealthEndpoint,
                readinessEndpoint = registration.ReadinessEndpoint,
                livenessEndpoint = registration.LivenessEndpoint,
                capabilities = registration.Capabilities,
                dependencies = registration.Dependencies,
                category = registration.Category,
                packageId = registration.PackageId,
                deploymentPackage = registration.DeploymentPackage,
                deploymentMetadata = registration.DeploymentMetadata,
                registeredAt = DateTimeOffset.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        });

        return endpoints;
    }
}
