namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// deployment.json content describing deployment behavior.
/// </summary>
public sealed class DeploymentDescription
{
    public required string ActivationMode { get; init; }

    public bool RequiresRestart { get; init; }

    public bool RollbackSupported { get; init; }

    public TimeSpan EstimatedDeploymentTime { get; init; }

    public TimeSpan EstimatedDowntime { get; init; }

    public required string DeploymentStrategy { get; init; }
}
