using Aovias.Release.Contracts.Deployment;

namespace Aovias.Release.SDK;

/// <summary>
/// Carries a structured <see cref="DeploymentFailure"/> through SDK and Command layers.
/// </summary>
public sealed class DeploymentFailureException : Exception
{
    public DeploymentFailureException(DeploymentFailure failure)
        : base(failure.Message)
    {
        Failure = failure;
    }

    public DeploymentFailure Failure { get; }
}
