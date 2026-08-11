using Aovias.Runtime.Contracts.Runtime;
using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Runtime.Queries;

/// <summary>
/// Query to retrieve the canonical runtime manifest.
/// </summary>
public sealed record GetRuntimeManifestQuery : IRequest<RuntimeManifest>;

/// <summary>
/// Handles <see cref="GetRuntimeManifestQuery"/>.
/// </summary>
public sealed class GetRuntimeManifestQueryHandler : IRequestHandler<GetRuntimeManifestQuery, RuntimeManifest>
{
    private readonly IRuntimeManifestProvider manifestProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRuntimeManifestQueryHandler"/> class.
    /// </summary>
    /// <param name="manifestProvider">The runtime manifest provider.</param>
    public GetRuntimeManifestQueryHandler(IRuntimeManifestProvider manifestProvider)
    {
        this.manifestProvider = manifestProvider;
    }

    /// <inheritdoc />
    public Task<RuntimeManifest> Handle(GetRuntimeManifestQuery request, CancellationToken cancellationToken)
        => manifestProvider.GetManifestAsync(cancellationToken);
}
