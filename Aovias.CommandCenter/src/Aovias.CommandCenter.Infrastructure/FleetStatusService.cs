using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Options;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// The Operations Command Center's single service layer (Sprint M2.7), implementing every one of
/// the brief's 11 required Command Center APIs plus Fleet Registry registration. Every read-only
/// fleet-wide method (<see cref="IFleetStatusService.GetFleetHealth"/>, <see cref="IFleetStatusService.GetFleetDrift"/>,
/// <see cref="IFleetStatusService.GetFleetReconciliation"/>, <see cref="IFleetStatusService.GetFleetMetrics"/>) reads only
/// <see cref="IFleetRegistry"/>/<see cref="IFleetHeartbeatCache"/> — never a live agent call — so
/// these are always fast and tolerant of offline agents. Only <see cref="GetFleetDeploymentHistoryAsync"/>,
/// <see cref="TriggerReconciliationAsync"/>, <see cref="SetMaintenanceModeAsync"/>, and
/// <see cref="ResumeFromMaintenanceAsync"/> call an agent live — each wrapped so one unreachable
/// agent never fails the whole request, and each one only ever calls an existing Runtime API
/// (Final Engineering Rule: "Command Center communicates only through Runtime APIs" / "Command
/// Center never performs provisioning directly").
/// </summary>
public interface IFleetStatusService
{
    RemoteEnvironment RegisterEnvironment(FleetRegistrationRequest request);

    IReadOnlyList<RemoteEnvironment> ListEnvironments(FleetSearchQuery? query = null);

    EnvironmentDetails? GetEnvironmentDetails(string environmentId);

    FleetHealthSnapshot GetFleetHealth();

    FleetDriftSnapshot GetFleetDrift();

    FleetReconciliationSnapshot GetFleetReconciliation();

    Task<FleetDeploymentHistorySnapshot> GetFleetDeploymentHistoryAsync(int limitPerEnvironment = 20, CancellationToken cancellationToken = default);

    FleetMetrics GetFleetMetrics();

    IReadOnlyList<FleetEvent> GetFleetEvents(string? environmentId = null, int limit = 100);

    Task<PlatformReconciliationReport> TriggerReconciliationAsync(string environmentId, CancellationToken cancellationToken = default);

    Task<MaintenanceModeStatus> SetMaintenanceModeAsync(string environmentId, MaintenanceModeRequest request, CancellationToken cancellationToken = default);

    Task<MaintenanceModeStatus> ResumeFromMaintenanceAsync(string environmentId, string resumedBy, CancellationToken cancellationToken = default);
}

public sealed class FleetStatusService : IFleetStatusService
{
    private readonly IFleetRegistry registry;
    private readonly IFleetHeartbeatCache heartbeatCache;
    private readonly IFleetDeploymentClientFactory clientFactory;
    private readonly IFleetEventStore eventStore;
    private readonly IOptionsMonitor<CommandCenterSettings> settings;

    public FleetStatusService(
        IFleetRegistry registry,
        IFleetHeartbeatCache heartbeatCache,
        IFleetDeploymentClientFactory clientFactory,
        IFleetEventStore eventStore,
        IOptionsMonitor<CommandCenterSettings> settings)
    {
        this.registry = registry;
        this.heartbeatCache = heartbeatCache;
        this.clientFactory = clientFactory;
        this.eventStore = eventStore;
        this.settings = settings;
    }

    public RemoteEnvironment RegisterEnvironment(FleetRegistrationRequest request)
    {
        var environment = registry.Register(request);
        eventStore.Append(new FleetEvent
        {
            Type = FleetEventType.AgentRegistered,
            EnvironmentId = environment.Id,
            Message = $"Environment '{environment.Name}' ({environment.BaseUrl}) was registered with the Fleet Registry."
        });

        return environment;
    }

    public IReadOnlyList<RemoteEnvironment> ListEnvironments(FleetSearchQuery? query = null)
        => query is null ? registry.List() : registry.Search(query);

    public EnvironmentDetails? GetEnvironmentDetails(string environmentId)
    {
        var environment = registry.Get(environmentId);
        if (environment is null)
        {
            return null;
        }

        return new EnvironmentDetails
        {
            Environment = environment,
            LatestHeartbeat = heartbeatCache.Get(environmentId),
            IsOnline = FleetOnlineStatus.IsOnline(environment, OfflineThreshold)
        };
    }

    public FleetHealthSnapshot GetFleetHealth()
    {
        var snapshots = registry.List().Select(environment =>
        {
            var heartbeat = heartbeatCache.Get(environment.Id);
            return new EnvironmentHealthSnapshot
            {
                EnvironmentId = environment.Id,
                Name = environment.Name,
                HealthStatus = heartbeat?.HealthStatus ?? (FleetOnlineStatus.IsOnline(environment, OfflineThreshold) ? environment.HealthStatus : "Unreachable"),
                PlatformVersion = environment.PlatformVersion,
                LastDeploymentUtc = heartbeat?.Deployment.LastDeploymentUtc ?? environment.LastDeploymentUtc
            };
        }).ToList();

        return new FleetHealthSnapshot { Environments = snapshots };
    }

    /// <summary>
    /// Builds one <see cref="EnvironmentDriftReport"/> per environment from the cached heartbeat's
    /// lightweight <see cref="ReconciliationSummaryDto"/> — a per-severity finding *count*, not
    /// individual resource-level findings (the heartbeat is deliberately lightweight and does not
    /// carry per-resource drift detail across the wire; see Sprint M2.6's richer, single-agent-only
    /// <c>DriftReport</c> for that). Each non-zero severity bucket becomes one synthetic
    /// <see cref="EnvironmentDriftFinding"/> summarizing that bucket's count (Sprint M2.7 Technical
    /// Debt: fleet-wide drift is a severity-bucketed summary, not a per-resource listing).
    /// </summary>
    public FleetDriftSnapshot GetFleetDrift()
    {
        var reports = new List<EnvironmentDriftReport>();
        var unreachable = new List<string>();

        foreach (var environment in registry.List())
        {
            var heartbeat = heartbeatCache.Get(environment.Id);
            if (heartbeat?.Reconciliation is null)
            {
                unreachable.Add(environment.Id);
                continue;
            }

            var findings = heartbeat.Reconciliation.DriftBySeverity
                .Where(pair => pair.Value > 0)
                .Select(pair => new EnvironmentDriftFinding
                {
                    Category = "Aggregate",
                    Resource = $"{pair.Value} finding(s)",
                    Expected = "No drift",
                    Actual = $"{pair.Value} finding(s) at {pair.Key} severity",
                    Severity = pair.Key
                })
                .ToList();

            reports.Add(new EnvironmentDriftReport
            {
                EnvironmentId = environment.Id,
                GeneratedUtc = heartbeat.Reconciliation.LastRunUtc ?? heartbeat.TimestampUtc,
                Findings = findings,
                RequiresReconciliation = heartbeat.Reconciliation.DriftFindingCount > 0
            });
        }

        return new FleetDriftSnapshot { Environments = reports, UnreachableEnvironmentIds = unreachable };
    }

    public FleetReconciliationSnapshot GetFleetReconciliation()
    {
        var statuses = registry.List().Select(environment => new EnvironmentReconciliationStatus
        {
            EnvironmentId = environment.Id,
            Summary = heartbeatCache.Get(environment.Id)?.Reconciliation
        }).ToList();

        return new FleetReconciliationSnapshot { Environments = statuses };
    }

    /// <summary>
    /// The one Fleet API that calls every agent live (<c>GET /api/deployment/history</c> — a
    /// pre-existing, environment-scoped endpoint with no ambiguous id parameter, unlike the
    /// legacy <c>health/{environmentId}</c>/<c>drift/{environmentId}</c> routes) — each call is
    /// independently try/caught so one unreachable agent only shrinks the result, never fails the
    /// whole request (Final Engineering Rule: "Fleet data must tolerate offline agents").
    /// </summary>
    public async Task<FleetDeploymentHistorySnapshot> GetFleetDeploymentHistoryAsync(int limitPerEnvironment = 20, CancellationToken cancellationToken = default)
    {
        var entries = new List<FleetDeploymentHistoryEntry>();
        var unreachable = new List<string>();

        foreach (var environment in registry.List())
        {
            try
            {
                var client = clientFactory.CreateClient(environment);
                var history = await client.GetHistoryAsync(cancellationToken);
                entries.AddRange(history
                    .Take(limitPerEnvironment)
                    .Select(entry => new FleetDeploymentHistoryEntry { EnvironmentId = environment.Id, Entry = entry }));
            }
            catch (Exception)
            {
                unreachable.Add(environment.Id);
            }
        }

        return new FleetDeploymentHistorySnapshot { Entries = entries, UnreachableEnvironmentIds = unreachable };
    }

    public FleetMetrics GetFleetMetrics()
    {
        var environments = registry.List();
        var online = environments.Count(environment => FleetOnlineStatus.IsOnline(environment, OfflineThreshold));

        return new FleetMetrics
        {
            TotalEnvironments = environments.Count,
            OnlineEnvironments = online,
            OfflineEnvironments = environments.Count - online,
            EnvironmentsWithDrift = environments.Count(environment => heartbeatCache.Get(environment.Id)?.Reconciliation?.DriftFindingCount > 0),
            EnvironmentsInMaintenance = environments.Count(environment => environment.MaintenanceMode),
            TotalOperatorApprovalsPending = environments.Sum(environment => heartbeatCache.Get(environment.Id)?.Reconciliation?.OperatorApprovalsPending ?? 0)
        };
    }

    public IReadOnlyList<FleetEvent> GetFleetEvents(string? environmentId = null, int limit = 100)
        => eventStore.GetRecent(environmentId, limit);

    public async Task<PlatformReconciliationReport> TriggerReconciliationAsync(string environmentId, CancellationToken cancellationToken = default)
    {
        var environment = RequireEnvironment(environmentId);
        var client = clientFactory.CreateClient(environment);
        var report = await client.ReconcileAsync(cancellationToken);

        eventStore.Append(new FleetEvent
        {
            Type = FleetEventType.ReconciliationTriggered,
            EnvironmentId = environmentId,
            Message = report.RequiresOperatorAttention
                ? $"Manual reconciliation triggered for '{environment.Name}' — operator attention required."
                : $"Manual reconciliation triggered for '{environment.Name}'."
        });

        return report;
    }

    public async Task<MaintenanceModeStatus> SetMaintenanceModeAsync(string environmentId, MaintenanceModeRequest request, CancellationToken cancellationToken = default)
    {
        var environment = RequireEnvironment(environmentId);
        var client = clientFactory.CreateClient(environment);
        var status = await client.SetMaintenanceModeAsync(request, cancellationToken);

        registry.Update(environmentId, current => current with
        {
            MaintenanceMode = status.Active,
            MaintenanceReason = status.Reason
        });

        eventStore.Append(new FleetEvent
        {
            Type = FleetEventType.MaintenanceEnabled,
            EnvironmentId = environmentId,
            Message = $"Environment '{environment.Name}' entered maintenance mode: {status.Reason ?? "no reason given"}."
        });

        return status;
    }

    public async Task<MaintenanceModeStatus> ResumeFromMaintenanceAsync(string environmentId, string resumedBy, CancellationToken cancellationToken = default)
    {
        var environment = RequireEnvironment(environmentId);
        var client = clientFactory.CreateClient(environment);
        var status = await client.ResumeFromMaintenanceAsync(resumedBy, cancellationToken);

        registry.Update(environmentId, current => current with
        {
            MaintenanceMode = status.Active,
            MaintenanceReason = status.Reason
        });

        eventStore.Append(new FleetEvent
        {
            Type = FleetEventType.MaintenanceResumed,
            EnvironmentId = environmentId,
            Message = $"Environment '{environment.Name}' resumed from maintenance mode by {resumedBy}."
        });

        return status;
    }

    private RemoteEnvironment RequireEnvironment(string environmentId)
        => registry.Get(environmentId)
           ?? throw new KeyNotFoundException($"Environment '{environmentId}' is not registered with the Fleet Registry.");

    private TimeSpan OfflineThreshold => TimeSpan.FromSeconds(Math.Max(5, settings.CurrentValue.OfflineThresholdSeconds));
}
