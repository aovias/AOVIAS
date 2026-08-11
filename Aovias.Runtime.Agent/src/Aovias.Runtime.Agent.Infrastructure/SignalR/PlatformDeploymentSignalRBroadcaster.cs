using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Application.Deployment;
using Aovias.Runtime.Contracts.Deployment;
using Microsoft.AspNetCore.SignalR;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// Streams platform deployment phase events to connected operators.
/// </summary>
public sealed class PlatformDeploymentSignalRBroadcaster : IPlatformDeploymentEventPublisher
{
    private readonly IHubContext<RuntimeHub> hubContext;

    public PlatformDeploymentSignalRBroadcaster(IHubContext<RuntimeHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task PublishAsync(DeploymentPhaseEventDto phaseEvent, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.All.SendAsync("DeploymentPhase", phaseEvent, cancellationToken);

        if (string.Equals(phaseEvent.Status, "Failed", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(phaseEvent.FailureCategory)
            && Enum.TryParse<DeploymentFailureCategory>(phaseEvent.FailureCategory, out var category))
        {
            var operatorError = DeploymentOperatorErrorMapper.Map(
                category,
                phaseEvent.Message,
                phaseEvent.ServiceId);

            await hubContext.Clients.All.SendAsync(
                "DeploymentFailure",
                new DeploymentOperatorErrorDto
                {
                    Category = operatorError.Category.ToString(),
                    Title = operatorError.Title,
                    Reason = phaseEvent.Message,
                    RecoveryAction = phaseEvent.RecoveryAction ?? operatorError.RecoveryAction,
                    DocumentationHint = phaseEvent.DocumentationHint ?? operatorError.DocumentationHint,
                    Phase = phaseEvent.Phase.ToString(),
                    Component = phaseEvent.Component ?? ResolveComponent(phaseEvent.Phase),
                    Operation = phaseEvent.Operation ?? string.Empty,
                    ExceptionType = phaseEvent.ExceptionType ?? string.Empty,
                    InnerExceptionMessage = phaseEvent.InnerExceptionMessage,
                    ServiceId = phaseEvent.ServiceId ?? operatorError.ServiceId,
                    RequiredSecret = operatorError.RequiredSecret,
                    DeploymentId = phaseEvent.DeploymentId,
                    HttpStatusCode = operatorError.HttpStatusCode
                },
                cancellationToken);
        }
    }

    private static string ResolveComponent(DeploymentPhaseKind phase)
        => phase switch
        {
            DeploymentPhaseKind.Upload => "DeploymentPackageStore",
            DeploymentPhaseKind.Provision => "InfrastructureProvisioningPipeline",
            DeploymentPhaseKind.Compose => "ReleaseManager",
            DeploymentPhaseKind.Health or DeploymentPhaseKind.Certification => "PlatformCertificationOrchestrator",
            DeploymentPhaseKind.Rollback => "ReleaseManager",
            _ => "PlatformDeploymentService"
        };
}
