using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;

namespace ComplianceServiceManager.Contracts.Publishing;

/// <summary>
/// Workspace adapter for the AOVIAS Publishing Engine in AOVIAS Command.
/// </summary>
public interface IPublishingWorkspaceService
{
    Task<IReadOnlyList<PublishingProjectOption>> DiscoverLocalProjectsAsync(CancellationToken cancellationToken = default);

    Task<ProjectDiscoveryResult?> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    string GetDefaultOutputDirectory();

    string GetDefaultRuntimeTarget();

    string GetActiveProfile();

    string GetReleaseLibraryPath();

    Task<PublishingResult> PublishAsync(
        PublishingWorkspaceRequest request,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<DeployPackageValidationResult> ValidatePackageAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<string> AddToReleaseLibraryAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeployableProject>> DiscoverDeployableProjectsAsync(CancellationToken cancellationToken = default);

    Task<SolutionPublishingResult> PublishSolutionAsync(
        SolutionPublishingRequest request,
        IProgress<SolutionPublishingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<SolutionPublishingPreflightResult> RunSolutionPreflightAsync(
        SolutionPublishingPreflightRequest request,
        IProgress<SolutionPublishingPreflightProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<SolutionPackageInspectionResult> InspectReleaseLibraryAsync(
        string releaseLibraryRoot,
        string? profile = null,
        CancellationToken cancellationToken = default);

    /// <summary>Read-only Release Library history for a service, for operator-facing version validation.</summary>
    Task<IReadOnlyList<ReleaseArtifactRecord>> GetReleaseVersionsAsync(
        string serviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads the Release Index and returns the newest on-disk Runtime Agent package,
    /// preferring certified entries when available.
    /// </summary>
    Task<PublishedArtifactSelection?> GetLatestRuntimeAgentArtifactAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Discovered project entry for the Publishing workspace selector.
/// </summary>
public sealed class PublishingProjectOption
{
    public required string Name { get; init; }

    public required string ProjectPath { get; init; }

    public required string DiscoveryPath { get; init; }

    public required string Kind { get; init; }

    public int? Port { get; init; }
}

/// <summary>
/// Publishing workspace publish request.
/// </summary>
public sealed class PublishingWorkspaceRequest
{
    public required string ProjectPath { get; init; }

    public required string ServiceId { get; init; }

    public required string Version { get; init; }

    public required string OutputDirectory { get; init; }

    public string Profile { get; init; } = "local";

    public string Publisher { get; init; } = Environment.UserName;
}
