using Aovias.Publishing.Contracts.ReleaseLibrary;

namespace Aovias.Publishing.Contracts;

/// <summary>
/// A single stage in the publishing pipeline.
/// </summary>
public interface IPublishingStep
{
    string Name { get; }

    Task<PublishingStepResult> ExecuteAsync(PublishingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Executes one canonical Sprint 8D pipeline stage.
/// </summary>
public interface IPublishingPipelineStage
{
    PublishingPipelineStageId StageId { get; }

    string StageName { get; }

    IReadOnlyList<PublishingPipelineStageId> Dependencies { get; }

    IReadOnlyList<string> ArtifactKeys { get; }

    Task<PublishingStageExecutionResult> ExecuteAsync(
        PublishingContext context,
        PublishingSession session,
        CancellationToken cancellationToken);
}

/// <summary>
/// Orchestrates stage-based publishing with replay, isolation, and structured diagnostics.
/// </summary>
public interface IPublishingPipelineOrchestrator
{
    Task<PublishingResult> ExecuteAsync(
        PublishingRequest request,
        IProgress<PublishingProgress>? progress = null,
        PublishingExecutionOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages publishing workspaces with create, reuse, clean, recover, and lock semantics.
/// </summary>
public interface IPublishingWorkspaceManager
{
    PublishingSession CreateSession(PublishingRequest request);

    void RecoverWorkspace(PublishingSession session);

    void CleanStaleWorkspaces(TimeSpan maxAge);

    void DisposeSession(PublishingSession session);
}

/// <summary>
/// Verifies generated packages and deterministic fingerprints (Sprint 8D D6).
/// </summary>
public interface IPackageVerifier
{
    Task<PackageVerificationReport> VerifyAsync(
        string packagePath,
        bool expectCertificate,
        bool expectDeterministicFingerprint,
        CancellationToken cancellationToken = default);

    Task<GoldenPackageComparisonResult> CompareToGoldenAsync(
        string packagePath,
        string serviceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores and compares verified golden deployment packages.
/// </summary>
public interface IGoldenPackageRepository
{
    Task StoreGoldenAsync(string serviceId, string packagePath, string fingerprint, CancellationToken cancellationToken = default);

    Task<GoldenPackageComparisonResult> CompareAsync(string serviceId, string packagePath, string fingerprint, CancellationToken cancellationToken = default);

    string? GetGoldenPackagePath(string serviceId);
}

/// <summary>
/// Writes structured publishing logs correlated by session id.
/// </summary>
public interface IPublishingLogWriter
{
    void Write(PublishingSession session, string logName, string content);

    string GetLogPath(PublishingSession session, string logName);
}

/// <summary>
/// Technology-specific project handler.
/// </summary>
public interface IProjectTypeProvider
{
    string ProviderName { get; }

    bool CanDiscover(string projectPath);

    Task<ProjectDiscoveryResult?> DiscoverAsync(string projectPath, CancellationToken cancellationToken);

    Task<PublishingStepResult> ValidateAsync(PublishingContext context, CancellationToken cancellationToken);

    Task<PublishingStepResult> RestoreDependenciesAsync(PublishingContext context, CancellationToken cancellationToken);

    Task<PublishingStepResult> BuildAsync(PublishingContext context, CancellationToken cancellationToken);

    Task<PublishingStepResult> PublishAsync(PublishingContext context, CancellationToken cancellationToken);

    Task<PublishingStepResult> InspectConfigurationAsync(PublishingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves the best provider for a discovered project.
/// </summary>
public interface IProjectTypeProviderRegistry
{
    IReadOnlyList<IProjectTypeProvider> Providers { get; }

    Task<ProjectDiscoveryResult?> DiscoverAsync(string projectPath, CancellationToken cancellationToken);

    IProjectTypeProvider Resolve(ProjectDiscoveryResult discovery);
}

/// <summary>
/// Reads and validates <c>.aovdeploy</c> packages.
/// </summary>
public interface IDeployPackageReader
{
    Task<DeployPackageContents?> ReadAsync(string packagePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates <c>.aovdeploy</c> packages.
/// </summary>
public interface IDeployPackageValidator
{
    Task<DeployPackageValidationResult> ValidateAsync(string packagePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Verifies a generated <c>.aovdeploy</c> package by unpacking it and asserting
/// every required artifact is present, parseable and checksum-consistent (D6).
/// </summary>
public interface IDeployPackageVerifier
{
    Task<DeployPackageVerificationResult> VerifyAsync(string packagePath, bool expectCertificate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds the final <c>.aovdeploy</c> archive.
/// </summary>
public interface IDeployPackageBuilder
{
    Task<PublishingStepResult> StageAsync(PublishingContext context, CancellationToken cancellationToken);

    Task<string> CreateInterimArchiveAsync(PublishingContext context, CancellationToken cancellationToken);

    Task<PublishingStepResult> FinalizeAsync(
        PublishingContext context,
        DeploymentReadinessReport report,
        CancellationToken cancellationToken);
}

/// <summary>
/// Generates deployment manifest JSON.
/// </summary>
public interface IDeployManifestGenerator
{
    DeployManifest Generate(PublishingContext context);
}

/// <summary>
/// Generates health definition JSON.
/// </summary>
public interface IHealthDefinitionGenerator
{
    DeployHealthDefinition Generate(PublishingContext context);
}

/// <summary>
/// Generates configuration template with secret placeholders.
/// </summary>
public interface IConfigTemplateGenerator
{
    Task<IReadOnlyDictionary<string, string>> GenerateAsync(PublishingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Generates the deployment environment template shipped with packages.
/// </summary>
public interface IEnvTemplateGenerator
{
    Task<string> GenerateAsync(PublishingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Generates docker-compose.yml from template.
/// </summary>
public interface IComposeGenerator
{
    Task<string> GenerateAsync(PublishingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Generates or validates Dockerfiles.
/// </summary>
public interface IDockerfileGenerator
{
    Task<PublishingStepResult> PrepareDockerfileAsync(PublishingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Orchestrates the publishing pipeline.
/// </summary>
public interface IPublishingPipeline
{
    Task<PublishingResult> ExecuteAsync(PublishingRequest request, IProgress<PublishingProgress>? progress, CancellationToken cancellationToken);

    Task<PublishingResult> CompleteDeferredDeploymentReadinessAsync(
        PendingPublishValidation pending,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Action? sampleResourceUsage = null,
        Action<long>? recordFreedBytes = null);
}

/// <summary>
/// SDK entry point for publishing operations.
/// </summary>
public interface IPublishingClient
{
    Task<ProjectDiscoveryResult?> DiscoverProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    Task<PublishingResult> PublishAsync(PublishingRequest request, IProgress<PublishingProgress>? progress = null, CancellationToken cancellationToken = default);

    Task<PublishingResult> BuildDockerAsync(PublishingRequest request, IProgress<PublishingProgress>? progress = null, CancellationToken cancellationToken = default);

    Task<PublishingResult> CreateDeploymentPackageAsync(PublishingRequest request, IProgress<PublishingProgress>? progress = null, CancellationToken cancellationToken = default);

    Task<DeployPackageValidationResult> ValidateDeploymentPackageAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeployableProject>> DiscoverDeployableProjectsAsync(string? platformRoot = null, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Registers a finished <c>.aovdeploy</c> package into the Release Library at
    /// <paramref name="releaseLibraryRoot"/> and returns its stored path. Sprint
    /// M1.5: the single implementation behind this call is
    /// <c>IReleaseLibraryService.RegisterOrganizedCopyAsync</c> — this replaces the
    /// prior CSM-local flat-copy implementation of "add to release library" so
    /// there is exactly one code path that writes into the library.
    /// </summary>
    Task<string> RegisterInReleaseLibraryAsync(
        string releaseLibraryRoot,
        string packagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every Release Library entry registered for <paramref name="serviceId"/>, for operator-
    /// facing version validation (does this version already exist, what should the next version be)
    /// before a publish run starts. Read-only — never mutates the library.
    /// </summary>
    Task<IReadOnlyList<ReleaseArtifactRecord>> GetReleaseVersionsAsync(
        string releaseLibraryRoot,
        string serviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads the Release Index and returns the newest on-disk artifact for
    /// <paramref name="serviceId"/>, preferring certified entries when available.
    /// </summary>
    Task<PublishedArtifactSelection?> ResolveLatestPublishedArtifactAsync(
        string releaseLibraryRoot,
        string serviceId,
        CancellationToken cancellationToken = default);
}
