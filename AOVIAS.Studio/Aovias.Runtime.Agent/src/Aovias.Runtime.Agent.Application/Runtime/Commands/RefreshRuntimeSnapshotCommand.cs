using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Runtime.Commands;

/// <summary>
/// Refreshes the persisted runtime manifest from live system state.
/// </summary>
public sealed record RefreshRuntimeSnapshotCommand : IRequest<bool>;

/// <summary>
/// Refreshes the persisted runtime manifest from live system state.
/// </summary>
public sealed class RefreshRuntimeSnapshotCommandHandler : IRequestHandler<RefreshRuntimeSnapshotCommand, bool>
{
    private readonly IRuntimeManifestProvider manifestProvider;

    public RefreshRuntimeSnapshotCommandHandler(IRuntimeManifestProvider manifestProvider)
    {
        this.manifestProvider = manifestProvider;
    }

    /// <inheritdoc />
    public async Task<bool> Handle(RefreshRuntimeSnapshotCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await manifestProvider.PersistSnapshotAsync(cancellationToken);
        return true;
    }
}
