using Aovias.Runtime.Agent.Application.Deployment;
using Aovias.Runtime.Contracts.Deployment;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// No-op deployment event publisher used when SignalR is disabled.
/// </summary>
public sealed class NoOpPlatformDeploymentEventPublisher : IPlatformDeploymentEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(DeploymentPhaseEventDto phaseEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
