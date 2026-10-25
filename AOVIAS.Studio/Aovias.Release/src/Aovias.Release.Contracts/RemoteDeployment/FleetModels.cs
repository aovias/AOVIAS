namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Sprint M2.7 — Operations Command Center &amp; Fleet Management. These models are the wire
/// contract between the Command Center and every Runtime Agent it monitors: the Runtime Agent's
/// new heartbeat/maintenance endpoints return them, and the Command Center's own fleet-wide
/// aggregation endpoints compose them. Placed in this project (rather than a new Contracts
/// project) because it is already the established shared wire-contract library both sides
/// reference — <see cref="EnvironmentDriftReport"/>/<see cref="EnvironmentHealthSnapshot"/>/
/// <see cref="PlatformReconciliationReport"/> already live here for the same reason.
/// </summary>

/// <summary>A single, lightweight per-cycle summary of a Runtime Agent's own Sprint M2.6 reconciliation state — deliberately much smaller than a full <c>DriftReport</c>/<c>ReconciliationPlan</c>, since the heartbeat this travels in must stay lightweight.</summary>
public sealed class ReconciliationSummaryDto
{
    public DateTimeOffset? LastRunUtc { get; init; }

    public int DriftFindingCount { get; init; }

    public IReadOnlyDictionary<string, int> DriftBySeverity { get; init; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public int OperatorApprovalsPending { get; init; }

    public bool LastCycleSucceeded { get; init; }

    public double LastCycleDurationMs { get; init; }
}

/// <summary>A lightweight deployment-activity summary, sourced from the Runtime Agent's own already-in-memory <c>DeploymentReliabilityMetrics</c> snapshot — no new computation, only a smaller projection of it.</summary>
public sealed class DeploymentSummaryDto
{
    public int TotalDeployments { get; init; }

    public int SuccessfulDeployments { get; init; }

    public int FailedDeployments { get; init; }

    public DateTimeOffset? LastDeploymentUtc { get; init; }

    public string? LastDeploymentState { get; init; }
}

/// <summary>
/// The payload returned by a Runtime Agent's <c>GET /api/deployment/heartbeat</c> (Sprint M2.7).
/// Every field is either already held in memory or a single cheap read (the last reconciliation
/// history record, not the full history) — the brief's "heartbeat must be lightweight" and "must
/// not block deployment execution" requirements are satisfied by construction: nothing this
/// endpoint reads is guarded by the same locks/state a deployment or reconciliation cycle holds
/// while running.
/// </summary>
public sealed class RuntimeHeartbeatDto
{
    public required string EnvironmentId { get; init; }

    public required string RuntimeVersion { get; init; }

    /// <summary>Sprint M4.3: the platform (contract/schema) version this environment declares, sourced from the same <c>RuntimeSettings.PlatformVersion</c> the bootstrap deploy manifest already reports — additive field, existing readers unaffected by the default.</summary>
    public string PlatformVersion { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; }

    public string HealthStatus { get; init; } = "Unknown";

    public IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>
    /// Sprint M4.4 — this environment's current Deployment Ready state, as last computed by its own
    /// <c>EnvironmentAdmissionHostedService</c> against its declared
    /// <c>EnvironmentCapabilityProfile</c> and everything it has been asked to deploy so far.
    /// <see langword="true"/> for a freshly registered environment with nothing deployed yet (there
    /// is nothing yet to be unready for) — additive field, existing readers unaffected by the
    /// default.
    /// </summary>
    public bool DeploymentReady { get; init; } = true;

    /// <summary>The number of blocking (Missing) admission diagnostics behind <see cref="DeploymentReady"/> — 0 when ready. Full diagnostic detail (message/remediation) is fetched on demand from the Runtime Agent's own <c>GET /api/deployment/admission</c>, not carried in every heartbeat, the same "lightweight heartbeat" discipline <see cref="ReconciliationSummaryDto"/> already follows.</summary>
    public int AdmissionBlockerCount { get; init; }

    /// <summary>The current Desired State's identity (<c>{PlanId}:{ContentHash}</c>) — <see langword="null"/> if nothing has been deployed yet via the Sprint M2.4+ plan-based path.</summary>
    public string? DesiredStateVersion { get; init; }

    public ReconciliationSummaryDto? Reconciliation { get; init; }

    public DeploymentSummaryDto Deployment { get; init; } = new();

    public bool MaintenanceModeActive { get; init; }

    public string? MaintenanceReason { get; init; }
}

/// <summary>Request to place a Runtime Agent's environment into maintenance mode (Sprint M2.7).</summary>
public sealed class MaintenanceModeRequest
{
    public string? Reason { get; init; }

    public required string RequestedBy { get; init; }
}

/// <summary>Current maintenance-mode status — returned both by the Runtime Agent's own maintenance endpoints and cached by the Command Center's Fleet Registry (Sprint M2.7).</summary>
public sealed class MaintenanceModeStatus
{
    public bool Active { get; init; }

    public string? Reason { get; init; }

    public DateTimeOffset? ChangedUtc { get; init; }

    public string? ChangedBy { get; init; }
}

/// <summary>The structured fleet-level event vocabulary the Command Center emits (Sprint M2.7) — mirrors the discipline Sprint M2.6's <c>ReconciliationEvent</c> established for one agent, applied here at fleet scope.</summary>
public enum FleetEventType
{
    AgentRegistered,
    AgentOnline,
    AgentOffline,
    DriftDetected,
    MaintenanceEnabled,
    MaintenanceResumed,
    ReconciliationTriggered
}

public sealed class FleetEvent
{
    public required FleetEventType Type { get; init; }

    public required string EnvironmentId { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Fleet-wide aggregate counters (Sprint M2.7), computed entirely from the Fleet Registry's cached state — never a live call to every agent, so this endpoint is always fast and tolerant of offline agents.</summary>
public sealed class FleetMetrics
{
    public int TotalEnvironments { get; init; }

    public int OnlineEnvironments { get; init; }

    public int OfflineEnvironments { get; init; }

    public int EnvironmentsWithDrift { get; init; }

    public int EnvironmentsInMaintenance { get; init; }

    public int TotalOperatorApprovalsPending { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class FleetHealthSnapshot
{
    public IReadOnlyList<EnvironmentHealthSnapshot> Environments { get; init; } = [];

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class FleetDriftSnapshot
{
    public IReadOnlyList<EnvironmentDriftReport> Environments { get; init; } = [];

    /// <summary>Environment ids the Command Center could not reach when building this snapshot — present, not silently dropped, so an operator can tell "no drift" apart from "we don't know."</summary>
    public IReadOnlyList<string> UnreachableEnvironmentIds { get; init; } = [];

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class EnvironmentReconciliationStatus
{
    public required string EnvironmentId { get; init; }

    public ReconciliationSummaryDto? Summary { get; init; }
}

public sealed class FleetReconciliationSnapshot
{
    public IReadOnlyList<EnvironmentReconciliationStatus> Environments { get; init; } = [];

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class FleetDeploymentHistoryEntry
{
    public required string EnvironmentId { get; init; }

    public required PlatformDeploymentHistoryEntry Entry { get; init; }
}

public sealed class FleetDeploymentHistorySnapshot
{
    public IReadOnlyList<FleetDeploymentHistoryEntry> Entries { get; init; } = [];

    public IReadOnlyList<string> UnreachableEnvironmentIds { get; init; } = [];

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A registration request the Command Center accepts to add a new environment to its Fleet
/// Registry (Sprint M2.7) — distinct from <see cref="AgentRegistrationRequest"/> (a Runtime
/// Agent's own token-validated self-registration): this carries the API key that self-registration
/// already issued. Through Sprint M2.7 this was always operator-supplied; Sprint M4.3 adds no new
/// registration endpoint or payload shape — it only adds a caller: <c>EnvironmentRegistrationHostedService</c>
/// (Runtime Agent) now submits this same request automatically at startup instead of requiring an
/// operator to.
/// </summary>
public sealed class FleetRegistrationRequest
{
    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public required string BaseUrl { get; init; }

    public EnvironmentType EnvironmentType { get; init; }

    public string? ApiKey { get; init; }

    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Sprint M4.3: the environment's declared deployment capabilities at registration time, sourced from the same <c>RuntimeCapabilitiesProvider</c> the heartbeat already reports — additive field, existing callers (bootstrap tool, manual operator registration) unaffected by the default empty list.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}

/// <summary>
/// The Command Center's "Environment details" API response (Sprint M2.7) — the Fleet Registry's
/// own cached entry plus the single latest heartbeat received from that environment (or
/// <see langword="null"/> if none has ever succeeded). Kept as a thin composite rather than
/// widening <see cref="RemoteEnvironment"/> itself: the registry entry is the durable fleet-
/// membership record, the heartbeat is a volatile, frequently-replaced snapshot — conflating them
/// would make every heartbeat cycle a full registry rewrite for no benefit.
/// </summary>
public sealed class EnvironmentDetails
{
    public required RemoteEnvironment Environment { get; init; }

    public RuntimeHeartbeatDto? LatestHeartbeat { get; init; }

    public bool IsOnline { get; init; }
}
