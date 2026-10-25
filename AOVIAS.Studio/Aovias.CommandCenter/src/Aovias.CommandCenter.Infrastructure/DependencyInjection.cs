using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// Operations Command Center infrastructure layer dependency injection extensions (Sprint M2.7).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCommandCenterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();

        // RemoteDeploymentSettings is the same shared, defaults-only-if-unconfigured settings
        // type Aovias.Release.SDK.DeploymentClient already binds to elsewhere — it holds only
        // relative endpoint path templates (no base URL), so binding it here does not create a
        // second, divergent copy of anything environment-specific.
        services.AddOptions<RemoteDeploymentSettings>().Bind(configuration.GetSection(RemoteDeploymentSettings.SectionName));
        services.AddOptions<CommandCenterSettings>().Bind(configuration.GetSection(CommandCenterSettings.SectionName));

        services.AddSingleton<IFleetRegistry, FleetRegistry>();
        services.AddSingleton<IFleetHeartbeatCache, InMemoryFleetHeartbeatCache>();
        services.AddSingleton<IFleetEventStore, InMemoryFleetEventStore>();
        services.AddSingleton<IFleetDeploymentClientFactory, FleetDeploymentClientFactory>();
        services.AddSingleton<IFleetStatusService, FleetStatusService>();
        services.AddHostedService<RuntimeAgentPollingService>();

        return services;
    }
}
