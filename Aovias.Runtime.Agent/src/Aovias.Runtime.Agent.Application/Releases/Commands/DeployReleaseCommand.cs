using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Releases.Commands;

/// <summary>
/// Queues a release deployment for execution.
/// </summary>
public sealed record DeployReleaseCommand(DeployReleaseRequest Request) : IRequest<DeployReleaseResult>;

/// <summary>
/// Handles <see cref="DeployReleaseCommand"/>.
/// </summary>
public sealed class DeployReleaseCommandHandler : IRequestHandler<DeployReleaseCommand, DeployReleaseResult>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeployReleaseCommandHandler"/> class.
    /// </summary>
    public DeployReleaseCommandHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<DeployReleaseResult> Handle(DeployReleaseCommand request, CancellationToken cancellationToken)
        => releaseManager.DeployAsync(request.Request, cancellationToken);
}
