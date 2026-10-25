using Aovias.Runtime.Contracts.Observability;
using Aovias.Runtime.SDK;

namespace ComplianceServiceManager.Contracts.Observability;

/// <summary>
/// Observability platform service contract for the Command Center.
/// </summary>
public interface IObservabilityService
{
    Task<RuntimeStateDto> GetStateAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RuntimeTimelineEntryDto>> GetTimelineAsync(
        string? search = null,
        string? category = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RuntimeMetricsSnapshotDto>> GetMetricsAsync(int limit = 60, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContainerObservationDto>> GetContainersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkObservationDto>> GetNetworksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseObservationDto>> GetReleasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowObservationDto>> GetWorkflowsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// SDK-backed observability service.
/// </summary>
public sealed class ObservabilityAgentService : IObservabilityService
{
    private readonly IRuntimeAgentClient runtimeAgentClient;

    public ObservabilityAgentService(IRuntimeAgentClient runtimeAgentClient)
    {
        this.runtimeAgentClient = runtimeAgentClient;
    }

    /// <inheritdoc />
    public Task<RuntimeStateDto> GetStateAsync(CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeStateAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<RuntimeTimelineEntryDto>> GetTimelineAsync(
        string? search = null,
        string? category = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeTimelineAsync(search, category, limit, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<RuntimeMetricsSnapshotDto>> GetMetricsAsync(int limit = 60, CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeMetricsAsync(limit, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ContainerObservationDto>> GetContainersAsync(CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeContainersAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<NetworkObservationDto>> GetNetworksAsync(CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeNetworksAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseObservationDto>> GetReleasesAsync(CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeReleasesAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowObservationDto>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
        => runtimeAgentClient.GetRuntimeWorkflowsAsync(cancellationToken);
}
