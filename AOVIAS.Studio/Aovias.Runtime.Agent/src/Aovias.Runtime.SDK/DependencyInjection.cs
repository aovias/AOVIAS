using Aovias.Configuration.Extensions;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.SDK;

/// <summary>
/// SDK dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the runtime agent HTTP client SDK.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The merged AOVIAS configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRuntimeAgentClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAoviasConfiguration(configuration);

        services.AddHttpClient<IRuntimeAgentClient, RuntimeAgentClient>((provider, client) =>
        {
            var apiSettings = provider.GetRequiredService<IOptionsMonitor<ApiSettings>>().CurrentValue;
            var securitySettings = provider.GetRequiredService<IOptionsMonitor<SecuritySettings>>().CurrentValue;

            client.BaseAddress = new Uri(apiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(securitySettings.ApiKey))
            {
                client.DefaultRequestHeaders.Remove("X-Api-Key");
                client.DefaultRequestHeaders.Add("X-Api-Key", securitySettings.ApiKey);
            }
        });

        services.AddSingleton<IRuntimeHubClient, RuntimeHubClient>();

        return services;
    }
}
