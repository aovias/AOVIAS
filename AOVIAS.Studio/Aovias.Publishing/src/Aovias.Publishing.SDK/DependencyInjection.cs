using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Core.ReleaseLibrary;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Publishing.SDK;

/// <summary>
/// Default publishing client facade.
/// </summary>
public sealed class PublishingClient : IPublishingClient
{
    private readonly IPublishingPipeline publishingPipeline;
    private readonly IProjectTypeProviderRegistry providerRegistry;
    private readonly IDeployPackageValidator deployPackageValidator;
    private readonly ISolutionPublishingOrchestrator solutionOrchestrator;
    private readonly IDeployableProjectDiscoverer deployableProjectDiscoverer;
    private readonly ISolutionPublishingPreflight solutionPreflight;
    private readonly ISolutionPackageInspector packageInspector;
    private readonly IReleaseLibraryService releaseLibraryService;

    public PublishingClient(
        IPublishingPipeline publishingPipeline,
        IProjectTypeProviderRegistry providerRegistry,
        IDeployPackageValidator deployPackageValidator,
        ISolutionPublishingOrchestrator solutionOrchestrator,
        IDeployableProjectDiscoverer deployableProjectDiscoverer,
        ISolutionPublishingPreflight solutionPreflight,
        ISolutionPackageInspector packageInspector,
        IReleaseLibraryService releaseLibraryService)
    {
        this.publishingPipeline = publishingPipeline;
        this.providerRegistry = providerRegistry;
        this.deployPackageValidator = deployPackageValidator;
        this.solutionOrchestrator = solutionOrchestrator;
        this.deployableProjectDiscoverer = deployableProjectDiscoverer;
        this.solutionPreflight = solutionPreflight;
        this.packageInspector = packageInspector;
        this.releaseLibraryService = releaseLibraryService;
    }

    /// <inheritdoc />
    public Task<ProjectDiscoveryResult?> DiscoverProjectAsync(string projectPath, CancellationToken cancellationToken = default)
        => providerRegistry.DiscoverAsync(projectPath, cancellationToken);

    /// <inheritdoc />
    public Task<PublishingResult> PublishAsync(
        PublishingRequest request,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => publishingPipeline.ExecuteAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public Task<PublishingResult> BuildDockerAsync(
        PublishingRequest request,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => publishingPipeline.ExecuteAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public Task<PublishingResult> CreateDeploymentPackageAsync(
        PublishingRequest request,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => publishingPipeline.ExecuteAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<DeployableProject>> DiscoverDeployableProjectsAsync(string? platformRoot = null, CancellationToken cancellationToken = default)
        => deployableProjectDiscoverer.DiscoverAsync(platformRoot, cancellationToken);

    /// <inheritdoc />
    public Task<SolutionPublishingResult> PublishSolutionAsync(
        SolutionPublishingRequest request,
        IProgress<SolutionPublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => solutionOrchestrator.PublishSolutionAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public async Task<DeployPackageValidationResult> ValidateDeploymentPackageAsync(string packagePath, CancellationToken cancellationToken = default)
        => await deployPackageValidator.ValidateAsync(packagePath, cancellationToken);

    /// <inheritdoc />
    public Task<SolutionPublishingPreflightResult> RunSolutionPreflightAsync(
        SolutionPublishingPreflightRequest request,
        IProgress<SolutionPublishingPreflightProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => solutionPreflight.RunAsync(request, progress, cancellationToken);

    /// <inheritdoc />
    public Task<SolutionPackageInspectionResult> InspectReleaseLibraryAsync(
        string releaseLibraryRoot,
        string? profile = null,
        CancellationToken cancellationToken = default)
        => packageInspector.InspectReleaseLibraryAsync(releaseLibraryRoot, profile, cancellationToken);

    /// <inheritdoc />
    public Task<string> RegisterInReleaseLibraryAsync(
        string releaseLibraryRoot,
        string packagePath,
        CancellationToken cancellationToken = default)
        => releaseLibraryService.RegisterOrganizedCopyAsync(releaseLibraryRoot, packagePath, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseArtifactRecord>> GetReleaseVersionsAsync(
        string releaseLibraryRoot,
        string serviceId,
        CancellationToken cancellationToken = default)
    {
        await releaseLibraryService.LoadAsync(releaseLibraryRoot, cancellationToken);
        return releaseLibraryService.FindByService(releaseLibraryRoot, serviceId);
    }

    /// <inheritdoc />
    public async Task<PublishedArtifactSelection?> ResolveLatestPublishedArtifactAsync(
        string releaseLibraryRoot,
        string serviceId,
        CancellationToken cancellationToken = default)
    {
        await releaseLibraryService.LoadAsync(releaseLibraryRoot, cancellationToken);
        return LatestPublishedArtifactResolver.Resolve(
            releaseLibraryRoot,
            releaseLibraryService.FindByService(releaseLibraryRoot, serviceId));
    }
}

/// <summary>
/// Dependency injection extensions for the publishing SDK.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the AOVIAS Publishing Engine.
    /// </summary>
    public static IServiceCollection AddAoviasPublishing(this IServiceCollection services)
    {
        Core.DependencyInjection.AddPublishingCore(services);
        Providers.DotNet.DependencyInjection.AddDotNetPublishingProvider(services);
        Providers.Angular.DependencyInjection.AddAngularPublishingProvider(services);
        Providers.Static.DependencyInjection.AddStaticPublishingProvider(services);
        services.AddSingleton<IPublishingClient, PublishingClient>();
        return services;
    }
}
