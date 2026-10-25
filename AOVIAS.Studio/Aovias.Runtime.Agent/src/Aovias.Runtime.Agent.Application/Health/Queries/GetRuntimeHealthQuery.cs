using Aovias.Runtime.Contracts.Health;
using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Health.Queries;

/// <summary>
/// Query to retrieve runtime health information.
/// </summary>
public sealed record GetRuntimeHealthQuery : IRequest<RuntimeHealthDto>;

/// <summary>
/// Handles <see cref="GetRuntimeHealthQuery"/>.
/// </summary>
public sealed class GetRuntimeHealthQueryHandler : IRequestHandler<GetRuntimeHealthQuery, RuntimeHealthDto>
{
    private readonly IHealthProvider healthProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRuntimeHealthQueryHandler"/> class.
    /// </summary>
    /// <param name="healthProvider">The health provider.</param>
    public GetRuntimeHealthQueryHandler(IHealthProvider healthProvider)
    {
        this.healthProvider = healthProvider;
    }

    /// <inheritdoc />
    public Task<RuntimeHealthDto> Handle(GetRuntimeHealthQuery request, CancellationToken cancellationToken)
        => healthProvider.GetHealthAsync(cancellationToken);
}
