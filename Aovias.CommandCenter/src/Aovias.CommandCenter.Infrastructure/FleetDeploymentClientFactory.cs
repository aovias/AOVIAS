using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// Produces one <see cref="IDeploymentClient"/> per registered environment (Sprint M2.7) — the
/// Command Center's only channel to a Runtime Agent (Final Engineering Rule: "Command Center
/// communicates only through Runtime APIs"). <see cref="RemoteDeploymentSettings"/> holds only
/// relative endpoint path templates (no base URL), so the single injected
/// <see cref="IOptionsMonitor{TOptions}"/> is safely shared across every environment; only the
/// per-call <see cref="HttpClient.BaseAddress"/> and <c>X-Api-Key</c> header differ.
/// </summary>
public interface IFleetDeploymentClientFactory
{
    IDeploymentClient CreateClient(RemoteEnvironment environment);
}

public sealed class FleetDeploymentClientFactory : IFleetDeploymentClientFactory
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings;
    private readonly ILoggerFactory loggerFactory;

    public FleetDeploymentClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings,
        ILoggerFactory loggerFactory)
    {
        this.httpClientFactory = httpClientFactory;
        this.deploymentSettings = deploymentSettings;
        this.loggerFactory = loggerFactory;
    }

    public IDeploymentClient CreateClient(RemoteEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var httpClient = httpClientFactory.CreateClient(nameof(FleetDeploymentClientFactory));
        httpClient.BaseAddress = new Uri(environment.BaseUrl, UriKind.Absolute);
        httpClient.Timeout = TimeSpan.FromSeconds(deploymentSettings.CurrentValue.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(environment.ApiKey))
        {
            httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", environment.ApiKey);
        }

        return new DeploymentClient(httpClient, deploymentSettings, loggerFactory.CreateLogger<DeploymentClient>());
    }
}
