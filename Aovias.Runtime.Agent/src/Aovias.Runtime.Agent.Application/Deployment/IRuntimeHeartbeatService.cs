using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Application.Deployment;

/// <summary>
/// Sprint M2.7: produces this Runtime Agent's lightweight fleet heartbeat and exposes its
/// maintenance-mode state to the Operations Command Center (and any other caller of the Runtime
/// APIs). Deliberately a separate service from <see cref="IPlatformDeploymentService"/> (which
/// owns the deployment queue/workflow) — every member here only reads state that is already held
/// in memory, or performs a single cheap read (e.g. the latest reconciliation history record, not
/// the full history), so it can never share a code path with, or block on, an in-progress
/// deployment or reconciliation cycle. This satisfies the brief's "Heartbeat must be lightweight"
/// and "Heartbeats must not block deployment execution" rules by construction.
/// </summary>
public interface IRuntimeHeartbeatService
{
    /// <summary>Builds the current heartbeat payload for <c>GET /api/deployment/heartbeat</c>.</summary>
    Task<RuntimeHeartbeatDto> GetHeartbeatAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns this environment's current maintenance-mode status.</summary>
    MaintenanceModeStatus GetMaintenanceMode();

    /// <summary>Places this environment into maintenance mode, suspending automatic repair (Sprint M2.6 Safety Framework) until resumed.</summary>
    MaintenanceModeStatus SetMaintenanceMode(MaintenanceModeRequest request);

    /// <summary>Resumes this environment from maintenance mode, restoring normal autonomous self-healing.</summary>
    MaintenanceModeStatus ResumeFromMaintenance(string resumedBy);
}
