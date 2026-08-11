using System.IO;
using System.IO.Compression;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using ComplianceServiceManager.Contracts.Publishing;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Publishing;

/// <summary>
/// Wraps the publishing SDK for the AOVIAS Command Publishing workspace.
/// </summary>
public sealed class PublishingWorkspaceService : IPublishingWorkspaceService
{
    private readonly IPublishingClient publishingClient;
    private readonly ServiceDiscoveryService discoveryService;
    private readonly IOptionsMonitor<PublishingSettings> publishingSettings;
    private readonly IOptionsMonitor<AoviasPlatformOptions> platformOptions;

    public PublishingWorkspaceService(
        IPublishingClient publishingClient,
        ServiceDiscoveryService discoveryService,
        IOptionsMonitor<PublishingSettings> publishingSettings,
        IOptionsMonitor<AoviasPlatformOptions> platformOptions)
    {
        this.publishingClient = publishingClient;
        this.discoveryService = discoveryService;
        this.publishingSettings = publishingSettings;
        this.platformOptions = platformOptions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishingProjectOption>> DiscoverLocalProjectsAsync(CancellationToken cancellationToken = default)
    {
        var services = await discoveryService.DiscoverAsync(cancellationToken);
        return services
            .Select(MapProjectOption)
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public Task<ProjectDiscoveryResult?> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default)
        => publishingClient.DiscoverProjectAsync(projectPath, cancellationToken);

    /// <inheritdoc />
    public string GetDefaultOutputDirectory() => publishingSettings.CurrentValue.OutputFolder;

    /// <inheritdoc />
    public string GetDefaultRuntimeTarget() => publishingSettings.CurrentValue.DefaultRuntime;

    /// <inheritdoc />
    public string GetActiveProfile() => platformOptions.CurrentValue.ActiveProfile;

    /// <inheritdoc />
    public string GetReleaseLibraryPath()
        => Path.Combine(platformOptions.CurrentValue.ConfigRoot, "storage", "local", "packages");

    /// <inheritdoc />
    public Task<PublishingResult> PublishAsync(
        PublishingWorkspaceRequest request,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var publishRequest = new PublishingRequest
        {
            ProjectPath = request.ProjectPath,
            ServiceId = request.ServiceId,
            Version = request.Version,
            OutputDirectory = request.OutputDirectory,
            Profile = request.Profile,
            Publisher = request.Publisher
        };

        return publishingClient.PublishAsync(publishRequest, progress, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DeployPackageValidationResult> ValidatePackageAsync(string packagePath, CancellationToken cancellationToken = default)
        => publishingClient.ValidateDeploymentPackageAsync(packagePath, cancellationToken);

    /// <inheritdoc />
    public async Task<string> AddToReleaseLibraryAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
        {
            throw new FileNotFoundException("Published package was not found.", packagePath);
        }

        var validation = await publishingClient.ValidateDeploymentPackageAsync(packagePath, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Release Library accepts only verified deployment packages: "
                + string.Join("; ", validation.Errors));
        }

        using (var archive = ZipFile.OpenRead(packagePath))
        {
            if (archive.GetEntry(DeployPackageLayout.DeploymentReadinessCertificateFile) is null)
            {
                throw new InvalidOperationException(
                    "Release Library accepts only deployment-certified packages. "
                    + $"Missing {DeployPackageLayout.DeploymentReadinessCertificateFile}.");
            }
        }

        // Sprint M1.5: delegates to the single authoritative Release Library
        // implementation (IReleaseLibraryService, via the publishing client) instead
        // of performing its own flat file copy. This also means the package is now
        // registered with full lifecycle/metadata tracking rather than just landing
        // as an untracked file in the library folder.
        var libraryRoot = GetReleaseLibraryPath();
        Directory.CreateDirectory(libraryRoot);
        return await publishingClient.RegisterInReleaseLibraryAsync(libraryRoot, packagePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeployableProject>> DiscoverDeployableProjectsAsync(CancellationToken cancellationToken = default)
        => publishingClient.DiscoverDeployableProjectsAsync(platformRoot: null, cancellationToken);

    /// <inheritdoc />
    public Task<SolutionPublishingResult> PublishSolutionAsync(
        SolutionPublishingRequest request,
        IProgress<SolutionPublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => publishingClient.PublishSolutionAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public Task<SolutionPublishingPreflightResult> RunSolutionPreflightAsync(
        SolutionPublishingPreflightRequest request,
        IProgress<SolutionPublishingPreflightProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => publishingClient.RunSolutionPreflightAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public Task<SolutionPackageInspectionResult> InspectReleaseLibraryAsync(
        string releaseLibraryRoot,
        string? profile = null,
        CancellationToken cancellationToken = default)
        => publishingClient.InspectReleaseLibraryAsync(releaseLibraryRoot, profile, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseArtifactRecord>> GetReleaseVersionsAsync(
        string serviceId,
        CancellationToken cancellationToken = default)
        => await publishingClient.GetReleaseVersionsAsync(GetReleaseLibraryPath(), serviceId, cancellationToken);

    /// <inheritdoc />
    public Task<PublishedArtifactSelection?> GetLatestRuntimeAgentArtifactAsync(CancellationToken cancellationToken = default)
        => publishingClient.ResolveLatestPublishedArtifactAsync(GetReleaseLibraryPath(), "runtime-agent", cancellationToken);

    private static PublishingProjectOption MapProjectOption(ServiceInfoModel service)
    {
        var discoveryPath = service.Kind == ServiceKind.AngularWebApp
            ? service.WorkingDirectory
            : service.ProjectPath;

        return new PublishingProjectOption
        {
            Name = service.Name,
            ProjectPath = service.ProjectPath,
            DiscoveryPath = discoveryPath,
            Kind = service.DisplayKind,
            Port = service.Port
        };
    }
}
