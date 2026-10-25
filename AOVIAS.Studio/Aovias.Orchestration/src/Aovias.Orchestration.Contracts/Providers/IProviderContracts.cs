namespace Aovias.Orchestration.Contracts.Providers;

/// <summary>
/// Base provider contract for infrastructure abstractions.
/// </summary>
public interface IProvider
{
    string ProviderName { get; }

    bool IsAvailable { get; }
}

/// <summary>
/// Docker infrastructure provider.
/// </summary>
public interface IDockerProvider : IProvider
{
    Task<DockerContainerInfo?> InspectContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default);

    Task StartContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default);

    Task StopContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default);

    Task RestartContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default);

    Task<string> GetLogsAsync(string containerIdOrName, int tailLines, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DockerContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default);

    Task ComposeUpAsync(string composeFilePath, string projectName, CancellationToken cancellationToken = default);

    Task ComposeDownAsync(string composeFilePath, string projectName, CancellationToken cancellationToken = default);

    Task<DockerContainerInfo?> WaitForContainerHealthAsync(
        string containerIdOrName,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists Docker networks currently known to the engine (Sprint M2.6 D2 — Observed State
    /// Engine's network discovery). Additive to this interface; every existing implementation
    /// before Sprint M2.6 only needed container-level discovery, so this and
    /// <see cref="ListVolumesAsync"/> are the first genuinely new discovery capabilities this
    /// interface has needed since Sprint M2.0.
    /// </summary>
    Task<IReadOnlyList<DockerNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists Docker volumes currently known to the engine (Sprint M2.6 D2).</summary>
    Task<IReadOnlyList<DockerVolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Docker network if one with this name does not already exist (Sprint M2.6 D6 —
    /// the "recreate Docker network" self-healing action). Implementations must be idempotent:
    /// calling this against an already-existing network of the same name must be a safe no-op,
    /// not an error.
    /// </summary>
    Task EnsureNetworkExistsAsync(string networkName, CancellationToken cancellationToken = default);
}

/// <summary>Docker network summary (Sprint M2.6 D2).</summary>
public sealed class DockerNetworkInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Driver { get; init; }
}

/// <summary>Docker volume summary (Sprint M2.6 D2).</summary>
public sealed class DockerVolumeInfo
{
    public required string Name { get; init; }

    public required string Driver { get; init; }

    public required string Mountpoint { get; init; }
}

/// <summary>
/// Docker container summary.
/// </summary>
public sealed class DockerContainerInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Image { get; init; }

    public required string State { get; init; }

    public required string Status { get; init; }

    public string? Health { get; init; }
}

/// <summary>
/// File system provider abstraction.
/// </summary>
public interface IFileSystemProvider : IProvider
{
    Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

    Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Host system provider abstraction.
/// </summary>
public interface ISystemProvider : IProvider
{
    Task<SystemInfoSnapshot> GetSystemInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Host system snapshot.
/// </summary>
public sealed class SystemInfoSnapshot
{
    public required string Hostname { get; init; }

    public required string OperatingSystem { get; init; }

    public required string Kernel { get; init; }
}

/// <summary>
/// Health verification provider abstraction.
/// </summary>
public interface IHealthProvider : IProvider
{
    Task<HealthCheckResult> CheckEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    Task<HealthCheckResult> WaitForHealthAsync(
        string endpoint,
        int expectedStatusCode,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Health check result.
/// </summary>
public sealed class HealthCheckResult
{
    public required bool IsHealthy { get; init; }

    public required string Message { get; init; }

    public int StatusCode { get; init; }
}

/// <summary>
/// Network provider abstraction.
/// </summary>
public interface INetworkProvider : IProvider
{
    Task<bool> IsReachableAsync(string endpoint, CancellationToken cancellationToken = default);
}

/// <summary>
/// Nginx provider abstraction reserved for future integration.
/// </summary>
public interface INginxProvider : IProvider
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<NginxOperationResult> ConfigureRuntimeSiteAsync(CancellationToken cancellationToken = default);

    Task<NginxOperationResult> RestorePreviousSiteAsync(CancellationToken cancellationToken = default);

    Task<NginxOperationResult> ReloadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an nginx configuration operation.
/// </summary>
public sealed class NginxOperationResult
{
    public required bool IsSuccess { get; init; }

    public required string Message { get; init; }

    public string? ConfigPath { get; init; }

    public static NginxOperationResult Success(string message, string? configPath = null)
        => new() { IsSuccess = true, Message = message, ConfigPath = configPath };

    public static NginxOperationResult Failure(string message)
        => new() { IsSuccess = false, Message = message };
}

/// <summary>
/// Certificate provider abstraction reserved for future integration.
/// </summary>
public interface ICertificateProvider : IProvider
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
