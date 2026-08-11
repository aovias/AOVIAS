using Aovias.Deployment.Contracts.Smoke;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Validates packages, startup order, contracts, and smoke declarations for a published solution.
/// </summary>
public interface IPlatformDeploymentSimulationService
{
    Task<PlatformDeploymentSimulationResult> SimulateAsync(
        string releaseLibraryRoot,
        CancellationToken cancellationToken = default);
}
