using Aovias.Runtime.Contracts.Deployment;
using Aovias.Runtime.Contracts.Health;
using Aovias.Runtime.Contracts.Monitoring;
using Aovias.Runtime.Contracts.Observability;
using Aovias.Runtime.Contracts.Runtime;

namespace Aovias.Runtime.SDK;

/// <summary>
/// Client contract for the AOVIAS Runtime Agent control plane API.
/// </summary>
public interface IRuntimeAgentClient
{
    /// <summary>
    /// Gets the canonical runtime manifest.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The runtime manifest.</returns>
    Task<RuntimeManifest> GetRuntimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the runtime health snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The runtime health snapshot.</returns>
    Task<RuntimeHealthDto> GetRuntimeHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runtime host information.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime host information.</returns>
    Task<RuntimeInfoDto> GetRuntimeInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runtime version metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime version metadata.</returns>
    Task<RuntimeVersionDto> GetRuntimeVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runtime environment metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime environment metadata.</returns>
    Task<RuntimeEnvironmentDto> GetRuntimeEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets runtime monitoring statistics.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime statistics snapshot.</returns>
    Task<RuntimeStatisticsDto> GetRuntimeStatisticsAsync(CancellationToken cancellationToken = default);

    Task<RuntimeCapabilitiesDto> GetRuntimeCapabilitiesAsync(CancellationToken cancellationToken = default);

    Task<RuntimeStateDto> GetRuntimeStateAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RuntimeTimelineEntryDto>> GetRuntimeTimelineAsync(
        string? search = null,
        string? category = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RuntimeMetricsSnapshotDto>> GetRuntimeMetricsAsync(
        int limit = 60,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContainerObservationDto>> GetRuntimeContainersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkObservationDto>> GetRuntimeNetworksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseObservationDto>> GetRuntimeReleasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowObservationDto>> GetRuntimeWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployment API availability and SignalR discovery metadata.
    /// </summary>
    Task<RuntimeDeploymentAvailabilityDto> GetDeploymentAvailabilityAsync(CancellationToken cancellationToken = default);
}
