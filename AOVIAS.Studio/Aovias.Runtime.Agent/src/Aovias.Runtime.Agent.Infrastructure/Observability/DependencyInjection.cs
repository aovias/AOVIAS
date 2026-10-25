using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Observability.Collectors;
using Aovias.Runtime.Agent.Infrastructure.Observability.EventBus;
using Aovias.Runtime.Agent.Infrastructure.Observability.EventStore;
using Aovias.Runtime.Agent.Infrastructure.Observability.Metrics;
using Aovias.Runtime.Agent.Infrastructure.Observability.Services;
using Aovias.Runtime.Agent.Infrastructure.Observability.Snapshots;
using Aovias.Runtime.Agent.Infrastructure.Observability.StateStore;
using Aovias.Runtime.Agent.Infrastructure.Observability.Timeline;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Runtime.Agent.Infrastructure.Observability;

/// <summary>
/// Observability platform dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers native AOVIAS observability services.
    /// </summary>
    public static IServiceCollection AddObservabilityPlatform(this IServiceCollection services)
    {
        services.AddSingleton<IRuntimeEventBus, RuntimeEventBus>();
        services.AddSingleton<IRuntimeStateStore, RuntimeStateStore>();
        services.AddSingleton<IRuntimeTimelineStore, RuntimeTimelineStore>();
        services.AddSingleton<IRuntimeMetricsStore, RuntimeMetricsStore>();
        services.AddSingleton<IRuntimeEventStore, InMemoryRuntimeEventStore>();

        services.AddSingleton<DockerStateCollector>();
        services.AddSingleton<SystemMetricsCollector>();

        services.AddHostedService<RuntimeObservabilityBootstrapHostedService>();
        services.AddHostedService<RuntimeStateProjectionHandler>();
        services.AddHostedService<ObservabilitySignalRBroadcastService>();
        services.AddHostedService<RuntimeSnapshotHostedService>();
        services.AddHostedService<DockerEventListenerService>();

        return services;
    }
}
