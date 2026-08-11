using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Releases.Commands;

/// <summary>
/// Queues a release rollback for execution.
/// </summary>
public sealed record RollbackReleaseCommand(RollbackReleaseRequest Request) : IRequest<RollbackReleaseResult>;

/// <summary>
/// Handles <see cref="RollbackReleaseCommand"/>.
/// </summary>
public sealed class RollbackReleaseCommandHandler : IRequestHandler<RollbackReleaseCommand, RollbackReleaseResult>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackReleaseCommandHandler"/> class.
    /// </summary>
    public RollbackReleaseCommandHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<RollbackReleaseResult> Handle(RollbackReleaseCommand request, CancellationToken cancellationToken)
        => releaseManager.RollbackAsync(request.Request, cancellationToken);
}
