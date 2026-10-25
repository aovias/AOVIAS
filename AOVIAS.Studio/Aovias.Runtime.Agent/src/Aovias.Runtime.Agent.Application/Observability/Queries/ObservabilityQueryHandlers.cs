using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Observability.Queries;

internal sealed class GetRuntimeStateQueryHandler : IRequestHandler<GetRuntimeStateQuery, RuntimeStateDto>
{
    private readonly IRuntimeStateStore stateStore;

    public GetRuntimeStateQueryHandler(IRuntimeStateStore stateStore) => this.stateStore = stateStore;

    public Task<RuntimeStateDto> Handle(GetRuntimeStateQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(stateStore.GetSnapshot());
    }
}

internal sealed class GetRuntimeTimelineQueryHandler : IRequestHandler<GetRuntimeTimelineQuery, IReadOnlyList<RuntimeTimelineEntryDto>>
{
    private readonly IRuntimeTimelineStore timelineStore;

    public GetRuntimeTimelineQueryHandler(IRuntimeTimelineStore timelineStore) => this.timelineStore = timelineStore;

    public Task<IReadOnlyList<RuntimeTimelineEntryDto>> Handle(GetRuntimeTimelineQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(timelineStore.Query(request.SearchTerm, request.Category, request.Limit));
    }
}

internal sealed class GetRuntimeMetricsQueryHandler : IRequestHandler<GetRuntimeMetricsQuery, IReadOnlyList<RuntimeMetricsSnapshotDto>>
{
    private readonly IRuntimeMetricsStore metricsStore;

    public GetRuntimeMetricsQueryHandler(IRuntimeMetricsStore metricsStore) => this.metricsStore = metricsStore;

    public Task<IReadOnlyList<RuntimeMetricsSnapshotDto>> Handle(GetRuntimeMetricsQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(metricsStore.GetRecent(request.Limit));
    }
}

internal sealed class GetRuntimeContainersQueryHandler : IRequestHandler<GetRuntimeContainersQuery, IReadOnlyList<ContainerObservationDto>>
{
    private readonly IRuntimeStateStore stateStore;

    public GetRuntimeContainersQueryHandler(IRuntimeStateStore stateStore) => this.stateStore = stateStore;

    public Task<IReadOnlyList<ContainerObservationDto>> Handle(GetRuntimeContainersQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(stateStore.GetSnapshot().Containers);
    }
}

internal sealed class GetRuntimeNetworksQueryHandler : IRequestHandler<GetRuntimeNetworksQuery, IReadOnlyList<NetworkObservationDto>>
{
    private readonly IRuntimeStateStore stateStore;

    public GetRuntimeNetworksQueryHandler(IRuntimeStateStore stateStore) => this.stateStore = stateStore;

    public Task<IReadOnlyList<NetworkObservationDto>> Handle(GetRuntimeNetworksQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(stateStore.GetSnapshot().Networks);
    }
}

internal sealed class GetRuntimeReleasesQueryHandler : IRequestHandler<GetRuntimeReleasesQuery, IReadOnlyList<ReleaseObservationDto>>
{
    private readonly IRuntimeStateStore stateStore;

    public GetRuntimeReleasesQueryHandler(IRuntimeStateStore stateStore) => this.stateStore = stateStore;

    public Task<IReadOnlyList<ReleaseObservationDto>> Handle(GetRuntimeReleasesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(stateStore.GetSnapshot().Releases);
    }
}

internal sealed class GetRuntimeWorkflowsQueryHandler : IRequestHandler<GetRuntimeWorkflowsQuery, IReadOnlyList<WorkflowObservationDto>>
{
    private readonly IRuntimeStateStore stateStore;

    public GetRuntimeWorkflowsQueryHandler(IRuntimeStateStore stateStore) => this.stateStore = stateStore;

    public Task<IReadOnlyList<WorkflowObservationDto>> Handle(GetRuntimeWorkflowsQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(stateStore.GetSnapshot().Workflows);
    }
}
