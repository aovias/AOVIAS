using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Observability.Queries;

/// <summary>
/// Gets the current runtime observability state snapshot.
/// </summary>
public sealed record GetRuntimeStateQuery : IRequest<RuntimeStateDto>;

/// <summary>
/// Gets runtime timeline entries with optional filtering.
/// </summary>
public sealed record GetRuntimeTimelineQuery(
    string? SearchTerm = null,
    string? Category = null,
    int Limit = 100) : IRequest<IReadOnlyList<RuntimeTimelineEntryDto>>;

/// <summary>
/// Gets recent runtime metric snapshots.
/// </summary>
public sealed record GetRuntimeMetricsQuery(int Limit = 60) : IRequest<IReadOnlyList<RuntimeMetricsSnapshotDto>>;

/// <summary>
/// Gets observed containers from runtime state.
/// </summary>
public sealed record GetRuntimeContainersQuery : IRequest<IReadOnlyList<ContainerObservationDto>>;

/// <summary>
/// Gets observed networks from runtime state.
/// </summary>
public sealed record GetRuntimeNetworksQuery : IRequest<IReadOnlyList<NetworkObservationDto>>;

/// <summary>
/// Gets observed releases from runtime state.
/// </summary>
public sealed record GetRuntimeReleasesQuery : IRequest<IReadOnlyList<ReleaseObservationDto>>;

/// <summary>
/// Gets observed workflows from runtime state.
/// </summary>
public sealed record GetRuntimeWorkflowsQuery : IRequest<IReadOnlyList<WorkflowObservationDto>>;
