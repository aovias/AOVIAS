using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>Well-known resource type discriminators used across Drift/Repair models (Sprint M2.6). Kept as string constants, not an enum, so <see cref="DriftRepairMatrix"/> lookups and future new resource types never require a breaking enum change.</summary>
public static class ReconciliationResourceTypes
{
    public const string Container = "Container";
    public const string DockerNetwork = "DockerNetwork";
    public const string Volume = "Volume";
    public const string FilesystemPath = "FilesystemPath";
    /// <summary>A filesystem path holding certificate or secret material — permission drift here is classified more severely than an ordinary path (see <see cref="DriftRepairMatrix"/>).</summary>
    public const string SensitivePath = "SensitivePath";
    public const string SymbolicLink = "SymbolicLink";
    public const string Certificate = "Certificate";
    public const string Secret = "Secret";
    public const string ConfigurationKey = "ConfigurationKey";
    public const string HealthEndpoint = "HealthEndpoint";
    public const string Service = "Service";
    public const string PackageVersion = "PackageVersion";
}

/// <summary>The single container this service's plan expects to be running (Sprint M2.6 D1).</summary>
public sealed class DesiredContainerState
{
    public required string ServiceId { get; init; }

    public required string ContainerName { get; init; }

    public required string ImageReference { get; init; }

    public IReadOnlyList<int> Ports { get; init; } = [];

    public string? HealthEndpoint { get; init; }

    public required string RestartPolicy { get; init; }
}

public sealed class DesiredNetworkState
{
    public required string NetworkName { get; init; }

    public required bool External { get; init; }
}

public sealed class DesiredVolumeState
{
    public required string HostToken { get; init; }

    public required string ContainerPath { get; init; }

    public required bool ReadOnly { get; init; }
}

public sealed class DesiredFilesystemPathState
{
    public required string Path { get; init; }

    public required string Permission { get; init; }

    public required string Owner { get; init; }

    /// <summary>True when this path holds certificate or secret material — drives the <see cref="ReconciliationResourceTypes.SensitivePath"/> classification.</summary>
    public required bool IsSensitive { get; init; }
}

public sealed class DesiredSymbolicLinkState
{
    public required string LinkPath { get; init; }

    public required string TargetPath { get; init; }
}

public sealed class DesiredCertificateState
{
    public required string Id { get; init; }

    public required bool Required { get; init; }

    public required string MountPath { get; init; }

    public required string FileName { get; init; }
}

public sealed class DesiredSecretState
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required bool Required { get; init; }
}

public sealed class DesiredConfigurationState
{
    public required string Key { get; init; }

    public required bool Required { get; init; }
}

public sealed class DesiredHealthEndpointState
{
    public required string Id { get; init; }

    public required string Url { get; init; }

    public required int ExpectedStatus { get; init; }
}

public sealed class DesiredServiceState
{
    public required string ServiceId { get; init; }

    public required bool Required { get; init; }
}

/// <summary>
/// The immutable, authoritative operational target for one service in one environment (Sprint
/// M2.6 D1). Built exclusively from the already-approved <see cref="DeploymentPlan"/>, the
/// <see cref="InfrastructureContract"/> it was planned from, and the <see cref="ServiceManifest"/>
/// deployment metadata that produced it — never from anything observed at runtime (Final
/// Engineering Rule: "Desired State is the only authoritative operational target"). Every
/// property is <see langword="init"/>-only and every collection is a read-only list.
/// </summary>
public sealed class DesiredState
{
    public required string EnvironmentId { get; init; }

    public required string ServiceId { get; init; }

    /// <summary>The <see cref="DeploymentPlanMetadata.PlanId"/> this state was derived from — carried forward so a reconciliation cycle can detect when the plan itself has changed since the last cycle (see the Safety Framework's "deployment plan is obsolete" check).</summary>
    public required string PlanId { get; init; }

    public required string PlanContentHash { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<DesiredServiceState> Services { get; init; } = [];

    public IReadOnlyList<DesiredContainerState> Containers { get; init; } = [];

    public IReadOnlyList<DesiredNetworkState> Networks { get; init; } = [];

    public IReadOnlyList<DesiredVolumeState> Volumes { get; init; } = [];

    public IReadOnlyList<DesiredFilesystemPathState> FilesystemPaths { get; init; } = [];

    public IReadOnlyList<DesiredSymbolicLinkState> SymbolicLinks { get; init; } = [];

    public IReadOnlyList<DesiredCertificateState> Certificates { get; init; } = [];

    public IReadOnlyList<DesiredSecretState> Secrets { get; init; } = [];

    public IReadOnlyList<DesiredConfigurationState> ConfigurationKeys { get; init; } = [];

    public IReadOnlyList<DesiredHealthEndpointState> HealthEndpoints { get; init; } = [];
}

/// <summary>Builds a <see cref="DesiredState"/> exclusively from a <see cref="DeploymentPlan"/>, its <see cref="InfrastructureContract"/>, and its <see cref="ServiceManifest"/> deployment metadata (Sprint M2.6 D1). Never reads runtime state — see <see cref="DesiredStateProvider"/> for the only implementation.</summary>
public interface IDesiredStateProvider
{
    /// <param name="plan">The approved deployment plan to derive desired state from.</param>
    /// <param name="contract">The sealed infrastructure contract for the deployed service.</param>
    /// <param name="manifest">The service's deployment metadata.</param>
    /// <param name="generatedUtc">The timestamp to stamp on the produced <see cref="DesiredState"/> (caller-supplied for determinism).</param>
    /// <param name="resolvedNetworkName">
    /// The actual Docker network name this deployment resolved its <c>${AOVIAS_DOCKER_NETWORK}</c>-style
    /// token to (the same value <c>ReleaseDeploymentProvider.ResolveDockerNetworkName</c> already
    /// computes at deployment time). Optional — when omitted, the contract's raw, unresolved
    /// token is used as-is (see <see cref="DesiredStateProvider"/>'s remarks on why this cannot be
    /// resolved inside this shared library itself).
    /// </param>
    DesiredState BuildDesiredState(
        DeploymentPlan plan,
        InfrastructureContract contract,
        ServiceManifest manifest,
        DateTimeOffset generatedUtc,
        string? resolvedNetworkName = null);
}

/// <summary>
/// The single, pure, deterministic <see cref="IDesiredStateProvider"/> implementation (Sprint
/// M2.6 D1). Every field is copied straight from the plan/contract/manifest already in hand — no
/// I/O, no clock read (the caller supplies <c>generatedUtc</c>, mirroring the determinism
/// discipline <see cref="Planning.DeploymentPlanBuilder"/> established in Sprint M2.4), and no
/// inference from anything observed. Calling this twice with the same inputs always produces an
/// equal <see cref="DesiredState"/> (field-for-field, modulo <see cref="DesiredState.GeneratedUtc"/>).
/// </summary>
public sealed class DesiredStateProvider : IDesiredStateProvider
{
    public DesiredState BuildDesiredState(
        DeploymentPlan plan,
        InfrastructureContract contract,
        ServiceManifest manifest,
        DateTimeOffset generatedUtc,
        string? resolvedNetworkName = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(manifest);

        var networkName = resolvedNetworkName ?? ResolveNetworkName(contract);

        return new DesiredState
        {
            EnvironmentId = plan.Environment.EnvironmentId,
            ServiceId = plan.Metadata.ServiceId,
            PlanId = plan.Metadata.PlanId,
            PlanContentHash = plan.Diagnostics.ContentHash,
            GeneratedUtc = generatedUtc,
            Services = plan.Services
                .Select(service => new DesiredServiceState { ServiceId = service.ServiceId, Required = service.Required })
                .ToList(),
            Containers =
            [
                new DesiredContainerState
                {
                    ServiceId = plan.Metadata.ServiceId,
                    ContainerName = string.IsNullOrWhiteSpace(manifest.Docker.ContainerName) ? manifest.ServiceId : manifest.Docker.ContainerName,
                    ImageReference = manifest.Docker.Image,
                    Ports = manifest.Docker.Ports,
                    HealthEndpoint = manifest.Docker.HealthEndpoint,
                    RestartPolicy = manifest.Deployment.RestartPolicy
                }
            ],
            Networks = networkName is null ? [] :
            [
                new DesiredNetworkState { NetworkName = networkName, External = contract.Network.ExternalNetwork }
            ],
            Volumes = contract.Storage.Mounts
                .Select(mount => new DesiredVolumeState { HostToken = mount.HostToken, ContainerPath = mount.ContainerPath, ReadOnly = mount.ReadOnly })
                .ToList(),
            FilesystemPaths = BuildFilesystemPaths(contract, plan),
            SymbolicLinks = [],
            Certificates = contract.Certificates.Certificates
                .Select(cert => new DesiredCertificateState { Id = cert.Id, Required = cert.Required, MountPath = cert.MountPath, FileName = cert.FileName })
                .ToList(),
            Secrets = contract.Secrets.Secrets
                .Select(secret => new DesiredSecretState { Id = secret.Id, Name = secret.Name, Required = secret.Required })
                .ToList(),
            // Configuration key requirements live on ServiceDeploymentBundle.Configuration
            // (Sprint M2.0), which is not one of D1's three named Desired State sources
            // (Deployment Plan -> Infrastructure Contract -> Deployment Metadata) and is not
            // passed to this method — see the M2.6 report's Technical Debt section for why
            // configuration-key drift detection is therefore out of this sprint's real scope,
            // honestly left empty rather than fabricated.
            ConfigurationKeys = [],
            HealthEndpoints = BuildHealthEndpoints(manifest, plan)
        };
    }

    private static string? ResolveNetworkName(InfrastructureContract contract)
        // The contract stores a token (e.g. "${AOVIAS_DOCKER_NETWORK}"), not a resolved name — a
        // resolved name is only known at deployment time (see LegacyCapabilityBridge/host paths in
        // Sprint M2.0-M2.3). Desired State intentionally does not resolve this token itself (D1:
        // never infer from runtime context); the caller (Runtime Agent) resolves it the same way
        // ReleaseDeploymentProvider already does and passes it forward — see Technical Debt in the
        // M2.6 report for why the token is left unresolved here rather than duplicating that
        // resolution logic in this shared library.
        => string.IsNullOrWhiteSpace(contract.Network.NetworkNameToken) ? null : contract.Network.NetworkNameToken;

    private static IReadOnlyList<DesiredFilesystemPathState> BuildFilesystemPaths(InfrastructureContract contract, DeploymentPlan plan)
    {
        var sensitiveMountPaths = contract.Certificates.Certificates.Select(cert => cert.MountPath)
            .Concat(plan.Infrastructure.RequiredCertificates)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var paths = new List<DesiredFilesystemPathState>();
        foreach (var directory in contract.Filesystem.Directories.Concat(contract.Filesystem.ReadOnlyDirectories))
        {
            paths.Add(new DesiredFilesystemPathState
            {
                Path = directory.Path,
                Permission = directory.Permission,
                Owner = directory.Owner,
                IsSensitive = sensitiveMountPaths.Any(sensitivePath => directory.Path.StartsWith(sensitivePath, StringComparison.OrdinalIgnoreCase))
            });
        }

        return paths;
    }

    private static IReadOnlyList<DesiredHealthEndpointState> BuildHealthEndpoints(ServiceManifest manifest, DeploymentPlan plan)
    {
        var endpoints = new List<DesiredHealthEndpointState>();
        if (!string.IsNullOrWhiteSpace(manifest.Docker.HealthEndpoint))
        {
            endpoints.Add(new DesiredHealthEndpointState { Id = "primary-health-endpoint", Url = manifest.Docker.HealthEndpoint, ExpectedStatus = 200 });
        }

        foreach (var gate in plan.HealthGates)
        {
            endpoints.Add(new DesiredHealthEndpointState { Id = gate.Id, Url = manifest.Docker.HealthEndpoint, ExpectedStatus = 200 });
        }

        return endpoints;
    }
}
