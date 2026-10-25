using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Application.Deployment;
using Aovias.Runtime.Agent.Infrastructure.Admission;
using Aovias.Runtime.Agent.Infrastructure.Reliability;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>
/// The one real <see cref="IRuntimeHeartbeatService"/> (Sprint M2.7), composing only providers and
/// stores that already exist for other purposes — no new discovery, no new I/O beyond what those
/// pieces already do on their own:
/// <list type="bullet">
/// <item><see cref="RuntimeSettings"/> for this environment's stable self-identity (<c>ServerId</c>) and declared runtime version.</item>
/// <item><see cref="RuntimeCapabilitiesProvider"/> (Sprint M2.2-era) for the capability list — already computed from feature flags, no I/O.</item>
/// <item><see cref="LastDeploymentPlanStore"/> (Sprint M2.6 D1) for the current Desired State's identity — an in-memory read.</item>
/// <item><see cref="IReconciliationHistoryStore"/> (Sprint M2.6 D9), read with <c>limit: 1</c> — the single latest record, never the full history.</item>
/// <item><see cref="DeploymentReliabilityMetricsStore"/> (Sprint M2.0-era) — an existing in-memory snapshot.</item>
/// <item><see cref="MaintenanceModeStore"/> (Sprint M2.7) — an in-memory flag.</item>
/// <item><c>IHealthProvider</c> — the same on-demand health check the Runtime Agent's own health endpoint already performs; a single cheap Docker overview call, not a fresh reconciliation cycle.</item>
/// </list>
/// </summary>
public sealed class RuntimeHeartbeatService : IRuntimeHeartbeatService
{
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly RuntimeCapabilitiesProvider capabilitiesProvider;
    private readonly LastDeploymentPlanStore planStore;
    private readonly IReconciliationHistoryStore historyStore;
    private readonly DeploymentReliabilityMetricsStore metricsStore;
    private readonly MaintenanceModeStore maintenanceModeStore;
    private readonly Aovias.Runtime.Agent.Application.Interfaces.IHealthProvider healthProvider;
    private readonly EnvironmentAdmissionStore admissionStore;

    public RuntimeHeartbeatService(
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        RuntimeCapabilitiesProvider capabilitiesProvider,
        LastDeploymentPlanStore planStore,
        IReconciliationHistoryStore historyStore,
        DeploymentReliabilityMetricsStore metricsStore,
        MaintenanceModeStore maintenanceModeStore,
        Aovias.Runtime.Agent.Application.Interfaces.IHealthProvider healthProvider,
        EnvironmentAdmissionStore admissionStore)
    {
        this.runtimeSettings = runtimeSettings;
        this.capabilitiesProvider = capabilitiesProvider;
        this.planStore = planStore;
        this.historyStore = historyStore;
        this.metricsStore = metricsStore;
        this.maintenanceModeStore = maintenanceModeStore;
        this.healthProvider = healthProvider;
        this.admissionStore = admissionStore;
    }

    public async Task<RuntimeHeartbeatDto> GetHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        var settings = runtimeSettings.CurrentValue;
        var capabilities = capabilitiesProvider.GetCapabilities(settings.RuntimeVersion);
        var maintenance = maintenanceModeStore.GetStatus();
        var health = await healthProvider.GetHealthAsync(cancellationToken);
        // Sprint M4.4: the last report EnvironmentAdmissionHostedService computed — a cheap file
        // read, never a fresh evaluation inline in the heartbeat path (the same "heartbeat must be
        // lightweight" discipline every other field here already follows). No persisted report yet
        // (e.g. the hosted service's first cycle hasn't completed) reports Ready with zero
        // blockers, the same vacuous-true a freshly evaluated empty requirement set would produce.
        var admission = await admissionStore.TryLoadAsync(cancellationToken);

        return new RuntimeHeartbeatDto
        {
            EnvironmentId = settings.ServerId,
            RuntimeVersion = settings.RuntimeVersion,
            // Sprint M4.3: platform version reporting — sourced from the same authoritative
            // RuntimeSettings the bootstrap deploy manifest already reads (Aovias.Bootstrap.Host,
            // Sprint M4.1), not a new configuration surface.
            PlatformVersion = settings.PlatformVersion,
            TimestampUtc = DateTimeOffset.UtcNow,
            HealthStatus = health.Status,
            Capabilities = capabilities.Capabilities,
            DesiredStateVersion = ResolveDesiredStateVersion(),
            Reconciliation = await ResolveReconciliationSummaryAsync(cancellationToken),
            Deployment = ResolveDeploymentSummary(),
            MaintenanceModeActive = maintenance.Active,
            MaintenanceReason = maintenance.Reason,
            DeploymentReady = admission?.IsReady ?? true,
            AdmissionBlockerCount = admission?.MissingCount ?? 0
        };
    }

    public MaintenanceModeStatus GetMaintenanceMode() => maintenanceModeStore.GetStatus();

    public MaintenanceModeStatus SetMaintenanceMode(MaintenanceModeRequest request) => maintenanceModeStore.Enable(request);

    public MaintenanceModeStatus ResumeFromMaintenance(string resumedBy) => maintenanceModeStore.Resume(resumedBy);

    /// <summary>
    /// This Runtime Agent hosts one plan per service (<see cref="LastDeploymentPlanStore"/> is
    /// keyed by <c>serviceId</c>) but the heartbeat's <c>DesiredStateVersion</c> is a single field
    /// — the common case this environment is provisioning is one service per agent, so the first
    /// recorded plan is reported. A future sprint that needs a per-service breakdown in the
    /// heartbeat itself (rather than via the richer drift/reconciliation APIs) would widen this to
    /// a list (Sprint M2.7 Technical Debt).
    /// </summary>
    private string? ResolveDesiredStateVersion()
    {
        var serviceId = planStore.ServiceIds.FirstOrDefault();
        if (serviceId is null)
        {
            return null;
        }

        var record = planStore.TryGet(serviceId);
        return record?.Plan.Metadata.PlanId;
    }

    private async Task<ReconciliationSummaryDto?> ResolveReconciliationSummaryAsync(CancellationToken cancellationToken)
    {
        var latest = (await historyStore.GetHistoryAsync(serviceId: null, limit: 1, cancellationToken: cancellationToken)).FirstOrDefault();
        if (latest is null)
        {
            return null;
        }

        return new ReconciliationSummaryDto
        {
            LastRunUtc = latest.TimestampUtc,
            DriftFindingCount = latest.DriftFindingCount,
            DriftBySeverity = latest.DriftBySeverity.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            OperatorApprovalsPending = latest.OperatorApprovalsRequested,
            LastCycleSucceeded = latest.RepairDecisions.All(decision => decision.Succeeded != false),
            LastCycleDurationMs = latest.DurationMs
        };
    }

    private DeploymentSummaryDto ResolveDeploymentSummary()
    {
        var snapshot = metricsStore.GetSnapshot();
        var last = metricsStore.GetLastDeployment();

        return new DeploymentSummaryDto
        {
            TotalDeployments = snapshot.TotalDeployments,
            SuccessfulDeployments = snapshot.SuccessfulDeployments,
            FailedDeployments = snapshot.FailedDeployments,
            LastDeploymentUtc = last?.CompletedUtc ?? last?.StartedUtc,
            LastDeploymentState = last?.FinalState.ToString()
        };
    }
}
