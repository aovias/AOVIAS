using Aovias.Runtime.Contracts.Runtime;
using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Runtime.Queries;

/// <summary>
/// Query to retrieve runtime environment metadata.
/// </summary>
public sealed record GetRuntimeEnvironmentQuery : IRequest<RuntimeEnvironmentDto>;

/// <summary>
/// Handles <see cref="GetRuntimeEnvironmentQuery"/>.
/// </summary>
public sealed class GetRuntimeEnvironmentQueryHandler : IRequestHandler<GetRuntimeEnvironmentQuery, RuntimeEnvironmentDto>
{
    private readonly IRuntimeProvider runtimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRuntimeEnvironmentQueryHandler"/> class.
    /// </summary>
    /// <param name="runtimeProvider">The runtime provider.</param>
    public GetRuntimeEnvironmentQueryHandler(IRuntimeProvider runtimeProvider)
    {
        this.runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc />
    public Task<RuntimeEnvironmentDto> Handle(GetRuntimeEnvironmentQuery request, CancellationToken cancellationToken)
        => runtimeProvider.GetRuntimeEnvironmentAsync(cancellationToken);
}
