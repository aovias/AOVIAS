using Aovias.Configuration.Extensions;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aovias.Release.SDK;

/// <summary>
/// Release SDK dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the release management HTTP client SDK.
    /// </summary>
    public static IServiceCollection AddReleaseClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAoviasConfiguration(configuration);

        services.AddHttpClient<IReleaseClient, ReleaseClient>((provider, client) =>
        {
            var apiSettings = provider.GetRequiredService<IOptionsMonitor<ApiSettings>>().CurrentValue;
            var releaseSettings = provider.GetRequiredService<IOptionsMonitor<ReleaseManagementSettings>>().CurrentValue;
            var securitySettings = provider.GetRequiredService<IOptionsMonitor<SecuritySettings>>().CurrentValue;

            client.BaseAddress = new Uri(apiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(releaseSettings.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(securitySettings.ApiKey))
            {
                client.DefaultRequestHeaders.Remove("X-Api-Key");
                client.DefaultRequestHeaders.Add("X-Api-Key", securitySettings.ApiKey);
            }
        });

        services.AddHttpClient<IDeploymentClient, DeploymentClient>((provider, client) =>
        {
            var apiSettings = provider.GetRequiredService<IOptionsMonitor<ApiSettings>>().CurrentValue;
            var deploymentSettings = provider.GetRequiredService<IOptionsMonitor<RemoteDeploymentSettings>>().CurrentValue;
            var securitySettings = provider.GetRequiredService<IOptionsMonitor<SecuritySettings>>().CurrentValue;

            client.BaseAddress = new Uri(apiSettings.BaseUrl);
            // Upload calls apply package-size-specific per-request cancellation. The handler
            // timeout must not cut off the configured 10-minute large-package window.
            client.Timeout = TimeSpan.FromSeconds(
                Math.Max(deploymentSettings.TimeoutSeconds, deploymentSettings.LargeUploadTimeoutSeconds));

            if (!string.IsNullOrWhiteSpace(securitySettings.ApiKey))
            {
                client.DefaultRequestHeaders.Remove("X-Api-Key");
                client.DefaultRequestHeaders.Add("X-Api-Key", securitySettings.ApiKey);
            }
        });

        return services;
    }
}
