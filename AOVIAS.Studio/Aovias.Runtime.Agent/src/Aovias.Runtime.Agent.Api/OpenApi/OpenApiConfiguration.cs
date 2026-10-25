using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Aovias.Runtime.Agent.Api.OpenApi;

/// <summary>
/// Configures OpenAPI and Swagger metadata for the runtime agent API.
/// </summary>
public static class OpenApiConfiguration
{
    /// <summary>
    /// Adds OpenAPI services for the runtime agent API.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRuntimeAgentOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AOVIAS Runtime Agent",
                Version = "1.0",
                Description = "Runtime control plane API for the AOVIAS platform."
            });

            var xmlFile = $"{typeof(OpenApiConfiguration).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Enables Swagger UI for the runtime agent API.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application.</returns>
    public static WebApplication UseRuntimeAgentOpenApi(this WebApplication app)
    {
        var apiSettings = app.Services.GetRequiredService<IConfiguration>().GetSection(ApiSettings.SectionName).Get<ApiSettings>();
        var pathBase = apiSettings?.PathBase?.TrimEnd('/') ?? string.Empty;
        var swaggerJsonPath = $"{pathBase}/swagger/v1/swagger.json";

        app.UseSwagger(options =>
        {
            if (!string.IsNullOrWhiteSpace(pathBase))
            {
                options.PreSerializeFilters.Add((document, _) =>
                {
                    document.Servers = [new OpenApiServer { Url = pathBase }];
                });
            }
        });
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(swaggerJsonPath, "AOVIAS Runtime Agent v1");
            options.DocumentTitle = "AOVIAS Runtime Agent";
            if (!string.IsNullOrWhiteSpace(pathBase))
            {
                options.RoutePrefix = $"{pathBase.TrimStart('/')}/swagger";
            }
        });

        return app;
    }
}
