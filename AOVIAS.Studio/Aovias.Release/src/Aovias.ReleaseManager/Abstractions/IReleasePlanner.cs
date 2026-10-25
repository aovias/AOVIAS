using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Generates deployment plans before execution.
/// </summary>
public interface IReleasePlanner
{
    Task<DeploymentPlan> CreatePlanAsync(ReleaseRecord release, CancellationToken cancellationToken = default);
}
