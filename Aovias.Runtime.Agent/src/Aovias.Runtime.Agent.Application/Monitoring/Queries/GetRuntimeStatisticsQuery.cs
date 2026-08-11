using Aovias.Runtime.Contracts.Monitoring;
using Aovias.Runtime.Agent.Application.Interfaces;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Monitoring.Queries;

/// <summary>
/// Query to retrieve runtime statistics.
/// </summary>
public sealed record GetRuntimeStatisticsQuery : IRequest<RuntimeStatisticsDto>;

/// <summary>
/// Handles <see cref="GetRuntimeStatisticsQuery"/>.
/// </summary>
public sealed class GetRuntimeStatisticsQueryHandler : IRequestHandler<GetRuntimeStatisticsQuery, RuntimeStatisticsDto>
{
    private readonly IStatisticsProvider statisticsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetRuntimeStatisticsQueryHandler"/> class.
    /// </summary>
    /// <param name="statisticsProvider">Statistics provider.</param>
    public GetRuntimeStatisticsQueryHandler(IStatisticsProvider statisticsProvider)
    {
        this.statisticsProvider = statisticsProvider;
    }

    /// <inheritdoc />
    public Task<RuntimeStatisticsDto> Handle(GetRuntimeStatisticsQuery request, CancellationToken cancellationToken)
        => statisticsProvider.GetStatisticsAsync(cancellationToken);
}
