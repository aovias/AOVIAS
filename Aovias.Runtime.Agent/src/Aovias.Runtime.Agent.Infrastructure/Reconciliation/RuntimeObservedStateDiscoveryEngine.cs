using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>
/// The one real <see cref="IObservedStateDiscoveryEngine"/> implementation (Sprint M2.6 D2),
/// backed by the same <see cref="IDockerProvider"/>/<see cref="IHealthProvider"/>/
/// <see cref="IFileSystemProvider"/> abstractions <c>ReleaseDeploymentProvider</c>'s
/// <c>RuntimeGateCheck</c> (Sprint M2.5) already uses — no new provider abstractions were invented
/// for this sprint. Every check here is strictly read-only (Final Engineering Rule: "Observed
/// State is read-only input to reconciliation") — this class never starts, stops, restarts, or
/// creates anything.
/// </summary>
public sealed class RuntimeObservedStateDiscoveryEngine : IObservedStateDiscoveryEngine
{
    private readonly IDockerProvider dockerProvider;
    private readonly IHealthProvider healthProvider;
    private readonly IFileSystemProvider fileSystemProvider;
    private readonly ILogger<RuntimeObservedStateDiscoveryEngine> logger;

    public RuntimeObservedStateDiscoveryEngine(
        IDockerProvider dockerProvider,
        IHealthProvider healthProvider,
        IFileSystemProvider fileSystemProvider,
        ILogger<RuntimeObservedStateDiscoveryEngine> logger)
    {
        this.dockerProvider = dockerProvider;
        this.healthProvider = healthProvider;
        this.fileSystemProvider = fileSystemProvider;
        this.logger = logger;
    }

    public async Task<ObservedState> DiscoverAsync(DesiredState desiredState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(desiredState);

        var allContainers = dockerProvider.IsAvailable
            ? await TryAsync(() => dockerProvider.ListContainersAsync(cancellationToken), [], "list containers")
            : [];
        var allNetworks = dockerProvider.IsAvailable
            ? await TryAsync(() => dockerProvider.ListNetworksAsync(cancellationToken), [], "list networks")
            : [];
        var allVolumes = dockerProvider.IsAvailable
            ? await TryAsync(() => dockerProvider.ListVolumesAsync(cancellationToken), [], "list volumes")
            : [];

        return new ObservedState
        {
            EnvironmentId = desiredState.EnvironmentId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = desiredState.Services.Select(service => DiscoverService(service, allContainers)).ToList(),
            Containers = desiredState.Containers.Select(container => DiscoverContainer(container, allContainers)).ToList(),
            Networks = desiredState.Networks.Select(network => DiscoverNetwork(network, allNetworks)).ToList(),
            Volumes = desiredState.Volumes.Select(volume => DiscoverVolume(volume, allVolumes)).ToList(),
            FilesystemPaths = await Task.WhenAll(desiredState.FilesystemPaths.Select(DiscoverFilesystemPathAsync)),
            SymbolicLinks = desiredState.SymbolicLinks.Select(DiscoverSymbolicLink).ToList(),
            Certificates = desiredState.Certificates.Select(DiscoverCertificate).ToList(),
            // No path is carried on DesiredSecretState (Sprint M2.6 Technical Debt — see
            // DesiredStateProvider) so secret existence cannot be genuinely checked yet.
            Secrets = desiredState.Secrets.Select(secret => new ObservedSecretState { Id = secret.Id, Exists = false, DiscoveryStatus = DiscoveryStatus.NotSupported }).ToList(),
            ConfigurationKeys = desiredState.ConfigurationKeys.Select(key => new ObservedConfigurationState { Key = key.Key, Present = false, DiscoveryStatus = DiscoveryStatus.NotSupported }).ToList(),
            HealthEndpoints = await Task.WhenAll(desiredState.HealthEndpoints.Select(endpoint => DiscoverHealthEndpointAsync(endpoint, cancellationToken))),
            AllDiscoveredContainerNames = allContainers.Select(c => c.Name).ToList(),
            AllDiscoveredNetworkNames = allNetworks.Select(n => n.Name).ToList(),
            AllDiscoveredVolumeNames = allVolumes.Select(v => v.Name).ToList()
        };
    }

    private ObservedServiceState DiscoverService(DesiredServiceState service, IReadOnlyList<DockerContainerInfo> allContainers)
    {
        if (!dockerProvider.IsAvailable)
        {
            return new ObservedServiceState { ServiceId = service.ServiceId, Running = false, DiscoveryStatus = DiscoveryStatus.Unavailable };
        }

        var running = allContainers.Any(c =>
            string.Equals(c.Name, service.ServiceId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.State, "running", StringComparison.OrdinalIgnoreCase));

        return new ObservedServiceState { ServiceId = service.ServiceId, Running = running, DiscoveryStatus = DiscoveryStatus.Observed };
    }

    private ObservedContainerState DiscoverContainer(DesiredContainerState container, IReadOnlyList<DockerContainerInfo> allContainers)
    {
        if (!dockerProvider.IsAvailable)
        {
            return new ObservedContainerState { ContainerName = container.ContainerName, Exists = false, DiscoveryStatus = DiscoveryStatus.Unavailable };
        }

        var match = allContainers.FirstOrDefault(c => string.Equals(c.Name, container.ContainerName, StringComparison.OrdinalIgnoreCase));
        return new ObservedContainerState
        {
            ContainerName = container.ContainerName,
            Exists = match is not null,
            State = match?.State,
            Status = match?.Status,
            Health = match?.Health,
            ImageReference = match?.Image,
            DiscoveryStatus = DiscoveryStatus.Observed
        };
    }

    private static ObservedNetworkState DiscoverNetwork(DesiredNetworkState network, IReadOnlyList<DockerNetworkInfo> allNetworks)
        => new()
        {
            NetworkName = network.NetworkName,
            Exists = allNetworks.Any(n => string.Equals(n.Name, network.NetworkName, StringComparison.OrdinalIgnoreCase)),
            DiscoveryStatus = DiscoveryStatus.Observed
        };

    private static ObservedVolumeState DiscoverVolume(DesiredVolumeState volume, IReadOnlyList<DockerVolumeInfo> allVolumes)
        => new()
        {
            HostToken = volume.HostToken,
            Exists = allVolumes.Any(v => string.Equals(v.Name, volume.HostToken, StringComparison.OrdinalIgnoreCase)),
            DiscoveryStatus = DiscoveryStatus.Observed
        };

    private async Task<ObservedFilesystemPathState> DiscoverFilesystemPathAsync(DesiredFilesystemPathState path)
    {
        try
        {
            var exists = await fileSystemProvider.DirectoryExistsAsync(path.Path);
            string? permission = null;
            if (exists)
            {
                permission = TryReadPermission(path.Path);
            }

            return new ObservedFilesystemPathState { Path = path.Path, Exists = exists, Permission = permission, DiscoveryStatus = DiscoveryStatus.Observed };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to discover filesystem path {Path}", path.Path);
            return new ObservedFilesystemPathState { Path = path.Path, Exists = false, DiscoveryStatus = DiscoveryStatus.Error };
        }
    }

    /// <summary>
    /// Reads a POSIX-style permission string (e.g. "775") directly via <see cref="File.GetUnixFileMode(string)"/>
    /// rather than through <see cref="IFileSystemProvider"/>, which has no permission-reading
    /// method today (Sprint M2.6 Technical Debt — a natural next addition to that interface).
    /// Returns <see langword="null"/> on any platform or access exception rather than throwing —
    /// permission drift simply cannot be detected in that case, which is honestly reflected by
    /// leaving <see cref="ObservedFilesystemPathState.Permission"/> unset.
    /// </summary>
    private string? TryReadPermission(string path)
    {
        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        try
        {
            var mode = File.GetUnixFileMode(path);
            return Convert.ToString((int)mode & 0x1FF, 8).PadLeft(3, '0');
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static ObservedSymbolicLinkState DiscoverSymbolicLink(DesiredSymbolicLinkState link)
    {
        try
        {
            var info = new FileInfo(link.LinkPath);
            var target = info.Exists ? info.LinkTarget : null;
            return new ObservedSymbolicLinkState { LinkPath = link.LinkPath, Exists = target is not null, ActualTargetPath = target, DiscoveryStatus = DiscoveryStatus.Observed };
        }
        catch (Exception)
        {
            return new ObservedSymbolicLinkState { LinkPath = link.LinkPath, Exists = false, DiscoveryStatus = DiscoveryStatus.Error };
        }
    }

    private static ObservedCertificateState DiscoverCertificate(DesiredCertificateState certificate)
    {
        try
        {
            var path = Path.Combine(certificate.MountPath, certificate.FileName);
            return new ObservedCertificateState { Id = certificate.Id, Exists = File.Exists(path), DiscoveryStatus = DiscoveryStatus.Observed };
        }
        catch (Exception)
        {
            return new ObservedCertificateState { Id = certificate.Id, Exists = false, DiscoveryStatus = DiscoveryStatus.Error };
        }
    }

    private async Task<ObservedHealthEndpointState> DiscoverHealthEndpointAsync(DesiredHealthEndpointState endpoint, CancellationToken cancellationToken)
    {
        if (!healthProvider.IsAvailable)
        {
            return new ObservedHealthEndpointState { Id = endpoint.Id, IsHealthy = false, DiscoveryStatus = DiscoveryStatus.Unavailable };
        }

        try
        {
            var result = await healthProvider.CheckEndpointAsync(endpoint.Url, cancellationToken);
            return new ObservedHealthEndpointState { Id = endpoint.Id, IsHealthy = result.IsHealthy, Message = result.Message, DiscoveryStatus = DiscoveryStatus.Observed };
        }
        catch (Exception exception)
        {
            return new ObservedHealthEndpointState { Id = endpoint.Id, IsHealthy = false, Message = exception.Message, DiscoveryStatus = DiscoveryStatus.Error };
        }
    }

    private async Task<IReadOnlyList<T>> TryAsync<T>(Func<Task<IReadOnlyList<T>>> action, IReadOnlyList<T> fallback, string what)
    {
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to {What} during observed state discovery.", what);
            return fallback;
        }
    }
}
