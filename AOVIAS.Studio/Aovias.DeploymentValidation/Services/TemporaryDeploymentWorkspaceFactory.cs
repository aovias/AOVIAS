using Aovias.DeploymentValidation.Models;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Creates temporary deployment workspaces for validation.
/// </summary>
public interface ITemporaryDeploymentWorkspaceFactory
{
    Task<TemporaryDeploymentWorkspace> CreateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc />
public sealed class TemporaryDeploymentWorkspaceFactory : ITemporaryDeploymentWorkspaceFactory
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ValidationWorkspaceManager workspaceManager;
    private readonly ILogger<TemporaryDeploymentWorkspace> logger;

    public TemporaryDeploymentWorkspaceFactory(
        IValidationCommandRunner commandRunner,
        ValidationWorkspaceManager workspaceManager,
        ILogger<TemporaryDeploymentWorkspace> logger)
    {
        this.commandRunner = commandRunner;
        this.workspaceManager = workspaceManager;
        this.logger = logger;
    }

    public Task<TemporaryDeploymentWorkspace> CreateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        if (context.UseStreamingValidation)
        {
            return workspaceManager.PrepareAsync(context, cancellationToken);
        }

        return TemporaryDeploymentWorkspace.CreateAsync(context, commandRunner, logger, cancellationToken);
    }
}
