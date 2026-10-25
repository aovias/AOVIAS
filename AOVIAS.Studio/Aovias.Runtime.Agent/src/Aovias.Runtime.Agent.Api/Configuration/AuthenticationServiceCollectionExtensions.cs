using Aovias.Runtime.Agent.Api.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Runtime.Agent.Api.Configuration;

/// <summary>
/// Registers authentication architecture components for future JWT support.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Adds authentication architecture services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRuntimeAgentAuthenticationArchitecture(this IServiceCollection services)
    {
        services.AddOptions<AuthenticationOptions>()
            .BindConfiguration(AuthenticationOptions.SectionName);

        services.AddSingleton<ITokenValidator, NoOpTokenValidator>();
        return services;
    }
}
