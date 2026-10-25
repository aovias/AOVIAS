using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Auth;

/// <summary>
/// Authorization for service-to-service internal endpoints via X-Internal-Api-Key header.
/// When no key is configured, only localhost requests are permitted (development).
/// </summary>
public static class InternalApiAuthorizationExtensions
{
    public const string InternalOnlyPolicy = "InternalOnly";

    public static IServiceCollection AddInternalApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(InternalOnlyPolicy, policy =>
            {
                policy.RequireAssertion(context =>
                {
                    if (context.Resource is not HttpContext httpContext)
                        return false;

                    return ValidateInternalApiKey(httpContext.Request, httpContext.RequestServices);
                });
            });
        });

        return services;
    }

    public static bool ValidateInternalApiKey(HttpRequest request, IServiceProvider services)
    {
        var providedKey = request.Headers["X-Internal-Api-Key"].FirstOrDefault();
        var configuration = services.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["InternalApi:ApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            var host = request.Host.Host;
            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                   || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                   || host.StartsWith("::1", StringComparison.OrdinalIgnoreCase);
        }

        return !string.IsNullOrEmpty(providedKey)
               && providedKey.Equals(expectedKey, StringComparison.Ordinal);
    }
}
