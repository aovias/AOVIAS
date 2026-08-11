namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>
/// How confidently a single observed fact was established (Sprint M2.6 D2). Mirrors the honesty
/// discipline Sprint M2.5's <c>PendingExternalExecutionTaskProvisioner</c> established: a
/// discovery engine that cannot genuinely check something must say so, never fabricate a
/// positive or negative result.
/// </summary>
public enum DiscoveryStatus
{
    /// <summary>The underlying provider was queried and returned a definitive answer.</summary>
    Observed,

    /// <summary>The underlying provider is unavailable (e.g. Docker disabled) — nothing was actually checked.</summary>
    Unavailable,

    /// <summary>No discovery capability exists for this resource kind yet (Sprint M2.6 Technical Debt).</summary>
    NotSupported,

    /// <summary>The provider was queried but the query itself failed (e.g. threw an exception).</summary>
    Error
}

public sealed class ObservedContainerState
{
    public required string ContainerName { get; init; }

    public required bool Exists { get; init; }

    public string? State { get; init; }

    public string? Status { get; init; }

    public string? Health { get; init; }

    public string? ImageReference { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedNetworkState
{
    public required string NetworkName { get; init; }

    public required bool Exists { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedVolumeState
{
    public required string HostToken { get; init; }

    public required bool Exists { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedFilesystemPathState
{
    public required string Path { get; init; }

    public required bool Exists { get; init; }

    public string? Permission { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedSymbolicLinkState
{
    public required string LinkPath { get; init; }

    public required bool Exists { get; init; }

    public string? ActualTargetPath { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedCertificateState
{
    public required string Id { get; init; }

    public required bool Exists { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedSecretState
{
    public required string Id { get; init; }

    public required bool Exists { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedConfigurationState
{
    public required string Key { get; init; }

    public required bool Present { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedHealthEndpointState
{
    public required string Id { get; init; }

    public required bool IsHealthy { get; init; }

    public string? Message { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

public sealed class ObservedServiceState
{
    public required string ServiceId { get; init; }

    public required bool Running { get; init; }

    public required DiscoveryStatus DiscoveryStatus { get; init; }
}

/// <summary>
/// One immutable snapshot of live infrastructure state, taken for exactly one reconciliation
/// cycle (Sprint M2.6 D2). Every list here corresponds one-to-one with a <see cref="DesiredState"/>
/// list of the same resource kind, plus a small set of broader inventories
/// (<see cref="AllDiscoveredContainerNames"/>/<see cref="AllDiscoveredNetworkNames"/>/
/// <see cref="AllDiscoveredVolumeNames"/>) used only for "extra resource" detection (D3). This
/// class is read-only input to reconciliation (Final Engineering Rule: "Observed State is
/// read-only input to reconciliation") — nothing in this library ever mutates an
/// <see cref="ObservedState"/> once built, and no discovery engine may perform a repair while
/// building one.
/// </summary>
public sealed class ObservedState
{
    public required string EnvironmentId { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<ObservedServiceState> Services { get; init; } = [];

    public IReadOnlyList<ObservedContainerState> Containers { get; init; } = [];

    public IReadOnlyList<ObservedNetworkState> Networks { get; init; } = [];

    public IReadOnlyList<ObservedVolumeState> Volumes { get; init; } = [];

    public IReadOnlyList<ObservedFilesystemPathState> FilesystemPaths { get; init; } = [];

    public IReadOnlyList<ObservedSymbolicLinkState> SymbolicLinks { get; init; } = [];

    public IReadOnlyList<ObservedCertificateState> Certificates { get; init; } = [];

    public IReadOnlyList<ObservedSecretState> Secrets { get; init; } = [];

    public IReadOnlyList<ObservedConfigurationState> ConfigurationKeys { get; init; } = [];

    public IReadOnlyList<ObservedHealthEndpointState> HealthEndpoints { get; init; } = [];

    /// <summary>Every container name Docker currently reports, regardless of whether it is part of this service's Desired State — the basis for "extra resource" detection (D3).</summary>
    public IReadOnlyList<string> AllDiscoveredContainerNames { get; init; } = [];

    public IReadOnlyList<string> AllDiscoveredNetworkNames { get; init; } = [];

    public IReadOnlyList<string> AllDiscoveredVolumeNames { get; init; } = [];
}

/// <summary>
/// Performs continuous, read-only infrastructure discovery (Sprint M2.6 D2). Deliberately kept
/// free of any dependency on Runtime Agent's concrete providers (<c>IDockerProvider</c>,
/// <c>IHealthProvider</c>, filesystem I/O) — mirroring the same seam Sprint M2.5's
/// <c>IExecutionGateCheck</c> established, so this shared library never depends on
/// Runtime-Agent-specific types. The one real implementation
/// (<c>RuntimeObservedStateDiscoveryEngine</c>) lives in
/// <c>Aovias.Runtime.Agent.Infrastructure.Reconciliation</c>. Implementations must never perform a
/// repair action while discovering — discovery is strictly read-only (Final Engineering Rule 2).
/// </summary>
public interface IObservedStateDiscoveryEngine
{
    /// <summary>
    /// Produces one immutable <see cref="ObservedState"/> snapshot for <paramref name="desiredState"/>'s
    /// scope (D2: "Observed State must be immutable for each reconciliation cycle" — a caller must
    /// call this again, producing a new instance, for the next cycle rather than mutating a
    /// previous result).
    /// </summary>
    Task<ObservedState> DiscoverAsync(DesiredState desiredState, CancellationToken cancellationToken = default);
}
