using AccredaOne.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AccredaOne.Api.Foundation;

public static class FoundationEndpoints
{
    public static WebApplication MapFoundationEndpoints(this WebApplication app)
    {
        app.MapGet("/health", (IOptions<AccredaOneApiOptions> options) =>
        {
            var settings = options.Value;
            return Results.Ok(new
            {
                status = "Healthy",
                service = settings.ServiceName,
                phase = settings.Phase
            });
        });

        app.MapGet("/ready", (IOptions<AccredaOneApiOptions> options) =>
        {
            var settings = options.Value;
            return Results.Ok(new
            {
                status = "Ready",
                service = settings.ServiceName,
                checks = new[]
                {
                    new { name = "configuration", status = "pass" },
                    new { name = "database", status = "not_configured" }
                }
            });
        });

        var versioned = app.MapGroup("/api/{version}");
        versioned.MapGet("/foundation", (string version, IOptions<AccredaOneApiOptions> options) =>
        {
            var settings = options.Value;
            if (!string.Equals(version, settings.ApiVersion, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Unknown API version",
                    detail: $"Supported version is {settings.ApiVersion}.");
            }

            return Results.Ok(new
            {
                service = settings.ServiceName,
                version = settings.ApiVersion,
                phase = settings.Phase,
                database = "not_connected",
                authentication = "not_implemented"
            });
        });

        return app;
    }
}
