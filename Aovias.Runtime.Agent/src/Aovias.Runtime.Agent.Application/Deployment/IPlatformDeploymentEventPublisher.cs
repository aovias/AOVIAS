using Aovias.Runtime.Contracts.Deployment;

namespace Aovias.Runtime.Agent.Application.Deployment;

/// <summary>
/// Publishes platform deployment phase events to connected operators.
/// </summary>
public interface IPlatformDeploymentEventPublisher
{
    Task PublishAsync(DeploymentPhaseEventDto phaseEvent, CancellationToken cancellationToken = default);
}
