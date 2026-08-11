using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// Periodically polls every registered environment's lightweight heartbeat (Sprint M2.7) and
/// updates <see cref="IFleetHeartbeatCache"/>/<see cref="IFleetRegistry"/> — the one and only
/// writer of "is this environment currently reachable" state. Every per-environment call is
/// wrapped in its own try/catch: one unreachable agent never affects any other agent's poll, and
/// never throws out of <see cref="ExecuteAsync"/> (Final Engineering Rule: "Fleet data must
/// tolerate offline agents"). This loop only ever reads from each agent (<c>GetHeartbeatAsync</c>)
/// — it never provisions, deploys, or repairs anything itself (Final Engineering Rule: "Command
/// Center never performs provisioning directly").
/// </summary>
public sealed class RuntimeAgentPollingService : BackgroundService
{
    private readonly IFleetRegistry registry;
    private readonly IFleetDeploymentClientFactory clientFactory;
    private readonly IFleetHeartbeatCache heartbeatCache;
    private readonly IFleetEventStore eventStore;
    private readonly IOptionsMonitor<CommandCenterSettings> settings;
    private readonly ILogger<RuntimeAgentPollingService> logger;

    public RuntimeAgentPollingService(
        IFleetRegistry registry,
        IFleetDeploymentClientFactory clientFactory,
        IFleetHeartbeatCache heartbeatCache,
        IFleetEventStore eventStore,
        IOptionsMonitor<CommandCenterSettings> settings,
        ILogger<RuntimeAgentPollingService> logger)
    {
        this.registry = registry;
        this.clientFactory = clientFactory;
        this.heartbeatCache = heartbeatCache;
        this.eventStore = eventStore;
        this.settings = settings;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PollOnceAsync(stoppingToken);

            var interval = TimeSpan.FromSeconds(Math.Max(5, settings.CurrentValue.PollIntervalSeconds));
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }
    }

    /// <summary>Internal (not private) solely so <c>Aovias.DeploymentValidation.Tests</c> can drive a single poll cycle deterministically instead of racing the background loop's timer — no behavior change.</summary>
    internal async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var environment in registry.List())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var wasOnline = FleetOnlineStatus.IsOnline(environment, OfflineThreshold);

            try
            {
                var client = clientFactory.CreateClient(environment);
                var heartbeat = await client.GetHeartbeatAsync(cancellationToken);

                heartbeatCache.Set(environment.Id, heartbeat);
                registry.Update(environment.Id, current => current with
                {
                    RuntimeVersion = heartbeat.RuntimeVersion,
                    // Sprint M4.3: ride the existing heartbeat loop for capability/platform-version
                    // synchronization too — no second polling mechanism. A registered environment's
                    // capability list and platform version stay current the same way its runtime
                    // version and health status already do.
                    PlatformVersion = heartbeat.PlatformVersion,
                    Capabilities = heartbeat.Capabilities,
                    // Sprint M4.4: same rationale — ride the existing heartbeat loop rather than
                    // add a second sync mechanism for Deployment Ready state.
                    DeploymentReady = heartbeat.DeploymentReady,
                    AdmissionBlockerCount = heartbeat.AdmissionBlockerCount,
                    HealthStatus = heartbeat.HealthStatus,
                    MaintenanceMode = heartbeat.MaintenanceModeActive,
                    MaintenanceReason = heartbeat.MaintenanceReason,
                    LastHeartbeatUtc = heartbeat.TimestampUtc
                });

                if (!wasOnline)
                {
                    eventStore.Append(new FleetEvent
                    {
                        Type = FleetEventType.AgentOnline,
                        EnvironmentId = environment.Id,
                        Message = $"Environment '{environment.Name}' is reachable again."
                    });
                }

                if (heartbeat.Reconciliation is { DriftFindingCount: > 0 })
                {
                    eventStore.Append(new FleetEvent
                    {
                        Type = FleetEventType.DriftDetected,
                        EnvironmentId = environment.Id,
                        Message = $"Environment '{environment.Name}' reports {heartbeat.Reconciliation.DriftFindingCount} drift finding(s)."
                    });
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "RuntimeAgentPollingService could not reach environment {EnvironmentId} ({EnvironmentName}).",
                    environment.Id,
                    environment.Name);

                if (wasOnline)
                {
                    eventStore.Append(new FleetEvent
                    {
                        Type = FleetEventType.AgentOffline,
                        EnvironmentId = environment.Id,
                        Message = $"Environment '{environment.Name}' did not respond to a heartbeat request: {exception.Message}"
                    });
                }
            }
        }
    }

    private TimeSpan OfflineThreshold => TimeSpan.FromSeconds(Math.Max(5, settings.CurrentValue.OfflineThresholdSeconds));
}

/// <summary>
/// Derives "is this environment currently online" purely from the Fleet Registry's cached
/// <see cref="RemoteEnvironment.LastHeartbeatUtc"/> — never a live call (Final Engineering Rule:
/// "Fleet data must tolerate offline agents"). Shared by the polling loop (to detect
/// online/offline transitions for <see cref="FleetEvent"/>s) and <c>FleetStatusService</c> (to
/// compute <see cref="FleetMetrics"/>).
/// </summary>
public static class FleetOnlineStatus
{
    public static bool IsOnline(RemoteEnvironment environment, TimeSpan offlineThreshold)
        => environment.LastHeartbeatUtc is { } lastHeartbeat &&
           DateTimeOffset.UtcNow - lastHeartbeat <= offlineThreshold;
}
