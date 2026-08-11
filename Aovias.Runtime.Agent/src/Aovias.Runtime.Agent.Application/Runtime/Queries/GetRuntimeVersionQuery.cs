using Aovias.Runtime.Contracts.Runtime;
using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Runtime.Queries;

/// <summary>
/// Query to retrieve runtime version metadata.
/// </summary>
public sealed record GetRuntimeVersionQuery : IRequest<RuntimeVersionDto>;

/// <summary>
/// Handles <see cref="GetRuntimeVersionQuery"/>.
/// </summary>
public sealed class GetRuntimeVersionQueryHandler : IRequestHandler<GetRuntimeVersionQuery, RuntimeVersionDto>
{
    private readonly IRuntimeProvider runtimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRuntimeVersionQueryHandler"/> class.
    /// </summary>
    /// <param name="runtimeProvider">The runtime provider.</param>
    public GetRuntimeVersionQueryHandler(IRuntimeProvider runtimeProvider)
    {
        this.runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc />
    public Task<RuntimeVersionDto> Handle(GetRuntimeVersionQuery request, CancellationToken cancellationToken)
        => runtimeProvider.GetRuntimeVersionAsync(cancellationToken);
}
