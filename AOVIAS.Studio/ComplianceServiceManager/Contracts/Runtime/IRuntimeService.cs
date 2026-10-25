using ComplianceServiceManager.Models.Runtime;

namespace ComplianceServiceManager.Contracts.Runtime;

/// <summary>
/// Defines the contract for retrieving AOVIAS Runtime operational data.
/// </summary>
public interface IRuntimeService
{
    /// <summary>
    /// Retrieves runtime metadata for the connected agent.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The current runtime information snapshot.</returns>
    Task<RuntimeInfo> GetRuntimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the services managed by the runtime agent.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of runtime services.</returns>
    Task<IReadOnlyList<RuntimeService>> GetRuntimeServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent runtime events for the operational log.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of runtime events ordered newest first.</returns>
    Task<IReadOnlyList<RuntimeEvent>> GetRuntimeEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves host statistics for the connected runtime.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The current runtime statistics snapshot.</returns>
    Task<RuntimeStatistics> GetRuntimeStatisticsAsync(CancellationToken cancellationToken = default);
}
