using Aovias.Release.Contracts.RemoteDeployment;

namespace ComplianceServiceManager.Contracts.Deployment;

/// <summary>
/// Manages trusted deployment environments for AOVIAS Command.
/// </summary>
public interface IEnvironmentManager
{
    Task<IReadOnlyList<RemoteEnvironment>> GetEnvironmentsAsync(CancellationToken cancellationToken = default);

    Task<RemoteEnvironment?> GetEnvironmentAsync(string environmentId, CancellationToken cancellationToken = default);

    Task SaveEnvironmentAsync(RemoteEnvironment environment, CancellationToken cancellationToken = default);

    Task<RemoteEnvironment> RegisterEnvironmentAsync(
        AgentRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<EnvironmentHealthSnapshot?> RefreshHealthAsync(
        string environmentId,
        CancellationToken cancellationToken = default);

    Task<EnvironmentDriftReport> DetectDriftAsync(
        string environmentId,
        CancellationToken cancellationToken = default);
}
