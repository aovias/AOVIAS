using Aovias.Runtime.Contracts.Health;
using Aovias.Runtime.Contracts.Runtime;

namespace Aovias.Runtime.Agent.Application.Interfaces;

/// <summary>
/// Provides runtime manifest and discovery data for the control plane.
/// </summary>
public interface IRuntimeProvider
{
    /// <summary>
    /// Retrieves runtime host information.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime host information.</returns>
    Task<RuntimeInfoDto> GetRuntimeInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves runtime environment metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime environment metadata.</returns>
    Task<RuntimeEnvironmentDto> GetRuntimeEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves runtime version metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime version metadata.</returns>
    Task<RuntimeVersionDto> GetRuntimeVersionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides access to the canonical runtime manifest.
/// </summary>
public interface IRuntimeManifestProvider
{
    /// <summary>
    /// Retrieves the runtime manifest.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The runtime manifest.</returns>
    Task<RuntimeManifest> GetManifestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current runtime manifest snapshot to disk.
    /// </summary>
    Task PersistSnapshotAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides host system information for the runtime agent.
/// </summary>
public interface ISystemInformationProvider
{
    /// <summary>
    /// Retrieves operating system information for the host.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Operating system description.</returns>
    Task<string> GetOperatingSystemAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves kernel information for the host.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Kernel version description.</returns>
    Task<string> GetKernelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves live host metrics for the runtime dashboard.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>System metrics snapshot.</returns>
    Task<SystemMetricsSnapshot> GetSystemMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides health evaluation for the runtime control plane.
/// </summary>
public interface IHealthProvider
{
    /// <summary>
    /// Retrieves the current runtime health snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime health snapshot.</returns>
    Task<RuntimeHealthDto> GetHealthAsync(CancellationToken cancellationToken = default);
}
