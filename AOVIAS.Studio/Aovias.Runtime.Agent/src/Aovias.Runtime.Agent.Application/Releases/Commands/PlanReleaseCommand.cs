using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Releases.Commands;

/// <summary>
/// Generates a deployment plan for a release.
/// </summary>
public sealed record PlanReleaseCommand(PlanDeploymentRequest Request) : IRequest<DeploymentPlan>;

/// <summary>
/// Handles <see cref="PlanReleaseCommand"/>.
/// </summary>
public sealed class PlanReleaseCommandHandler : IRequestHandler<PlanReleaseCommand, DeploymentPlan>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanReleaseCommandHandler"/> class.
    /// </summary>
    public PlanReleaseCommandHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<DeploymentPlan> Handle(PlanReleaseCommand request, CancellationToken cancellationToken)
        => releaseManager.GenerateDeploymentPlanAsync(request.Request, cancellationToken);
}
