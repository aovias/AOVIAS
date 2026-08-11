using Aovias.Runtime.Contracts.Runtime;
using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Runtime.Queries;

/// <summary>
/// Query to retrieve runtime host information.
/// </summary>
public sealed record GetRuntimeInfoQuery : IRequest<RuntimeInfoDto>;

/// <summary>
/// Handles <see cref="GetRuntimeInfoQuery"/>.
/// </summary>
public sealed class GetRuntimeInfoQueryHandler : IRequestHandler<GetRuntimeInfoQuery, RuntimeInfoDto>
{
    private readonly IRuntimeProvider runtimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRuntimeInfoQueryHandler"/> class.
    /// </summary>
    /// <param name="runtimeProvider">The runtime provider.</param>
    public GetRuntimeInfoQueryHandler(IRuntimeProvider runtimeProvider)
    {
        this.runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc />
    public Task<RuntimeInfoDto> Handle(GetRuntimeInfoQuery request, CancellationToken cancellationToken)
        => runtimeProvider.GetRuntimeInfoAsync(cancellationToken);
}
