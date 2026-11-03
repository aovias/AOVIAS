using AccredaOne.Api.Configuration;
using AccredaOne.Api.Discovery.Contracts;
using Microsoft.Extensions.Options;

namespace AccredaOne.Api.Discovery;

public static class DiscoveryEndpoints
{
    public static WebApplication MapDiscoveryEndpoints(this WebApplication app)
    {
        var versioned = app.MapGroup("/api/{version}");

        versioned.MapPost("/discovery/query", (
            string version,
            DiscoveryQueryRequest? request,
            IOptions<AccredaOneApiOptions> options,
            IDiscoveryQueryService discoveryQueryService) =>
        {
            var settings = options.Value;
            if (!string.Equals(version, settings.ApiVersion, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Unknown API version",
                    detail: $"Supported version is {settings.ApiVersion}.");
            }

            if (request is null || string.IsNullOrWhiteSpace(request.Query))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["query"] = ["Query text is required."],
                });
            }

            var response = discoveryQueryService.Execute(request);
            return Results.Ok(response);
        });

        return app;
    }
}
