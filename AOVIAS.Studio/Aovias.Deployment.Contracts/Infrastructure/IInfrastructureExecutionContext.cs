namespace Aovias.Deployment.Contracts.Infrastructure;

/// <summary>
/// Executes host-level infrastructure actions required during provisioning.
/// Implemented by Runtime Agent; optional in DRV/offline validation.
/// </summary>
public interface IInfrastructureExecutionContext
{
    Task<DockerEngineStatus> InspectDockerEngineAsync(CancellationToken cancellationToken = default);

    Task EnsureDockerNetworkAsync(string networkName, CancellationToken cancellationToken = default);

    Task<bool> ImageExistsLocallyAsync(string imageReference, CancellationToken cancellationToken = default);

    public Task<DatabaseReachabilityResult> ValidateDatabaseAsync(
        string connectionString,
        CancellationToken cancellationToken = default);

    Task<MessageBusReachabilityResult> ValidateMessageBusAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}

public sealed class DockerEngineStatus
{
    public bool Available { get; init; }

    public bool ComposeAvailable { get; init; }

    public string? Version { get; init; }

    public string? Error { get; init; }
}

public sealed class DatabaseReachabilityResult
{
    public bool Reachable { get; init; }

    public string? Error { get; init; }

    public static DatabaseReachabilityResult Success() => new() { Reachable = true };

    public static DatabaseReachabilityResult Failure(string error) => new() { Reachable = false, Error = error };
}

public sealed class MessageBusReachabilityResult
{
    public bool Reachable { get; init; }

    public string? Error { get; init; }

    public static MessageBusReachabilityResult Success() => new() { Reachable = true };

    public static MessageBusReachabilityResult Failure(string error) => new() { Reachable = false, Error = error };
}
