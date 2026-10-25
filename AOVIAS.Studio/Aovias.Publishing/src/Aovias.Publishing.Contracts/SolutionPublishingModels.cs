namespace Aovias.Publishing.Contracts;

/// <summary>
/// Scans the platform repository and returns deployable applications.
/// </summary>
public interface IDeployableProjectDiscoverer
{
    Task<IReadOnlyList<DeployableProject>> DiscoverAsync(string? platformRoot = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes every deployable project in a solution with failure isolation.
/// </summary>
public interface ISolutionPublishingOrchestrator
{
    Task<SolutionPublishingResult> PublishSolutionAsync(
        SolutionPublishingRequest request,
        IProgress<SolutionPublishingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to publish the full platform solution.
/// </summary>
public sealed class SolutionPublishingRequest
{
    public string? PlatformRoot { get; init; }

    public string Profile { get; init; } = "demo";

    public string Version { get; init; } = "1.0.0";

    public string? OutputDirectory { get; init; }

    public string Publisher { get; init; } = Environment.UserName;

    public int? MaxConcurrency { get; init; }

    public bool GenerateDemoBundle { get; init; } = true;

    public bool OrganizeReleaseLibrary { get; init; } = true;

    public bool SkipPreflight { get; init; }

    public bool InspectPackagesAfterPublish { get; init; } = true;

    /// <summary>
    /// Fast, Balanced, or Full deployment validation mode.
    /// </summary>
    public string? DeploymentValidationMode { get; init; }

    /// <summary>
    /// Optional per-service version override, keyed by service ID (case-insensitive lookup by
    /// callers). When a service ID has an entry here, it is published at that version instead of
    /// <see cref="Version"/>. Lets an operator-controlled semantic-versioning workflow assign each
    /// service its own next version (e.g. a patch bump computed from its own Release Library
    /// history) without changing the meaning of <see cref="Version"/> for existing callers that
    /// leave this null.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ServiceVersionOverrides { get; init; }
}

/// <summary>
/// Outcome of a full-solution publish run.
/// </summary>
public sealed class SolutionPublishingResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<SolutionProjectPublishResult> ProjectResults { get; init; } = [];

    public string? SolutionDeploymentManifestPath { get; init; }

    public string? DemoDeploymentBundlePath { get; init; }

    public string? SummaryLogPath { get; init; }

    public string? PublishingLogDirectory { get; init; }

    public int SucceededCount { get; init; }

    public int FailedCount { get; init; }

    public int DeploymentReadyCount { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public SolutionPublishingPreflightResult? PreflightResult { get; init; }

    public SolutionPackageInspectionResult? PackageInspectionResult { get; init; }

    public PublishingResourceSnapshot? ResourceSnapshot { get; init; }

    public bool PlatformSimulationPassed { get; init; }

    public string? PlatformSimulationMessage { get; init; }
}

/// <summary>
/// Disk usage snapshot during publishing.
/// </summary>
public sealed class PublishingResourceSnapshot
{
    public long CurrentAvailableBytes { get; init; }

    public long PeakConsumedBytes { get; init; }

    public long FreedBytes { get; init; }

    public long RemainingBytes { get; init; }
}

/// <summary>
/// Per-project publish outcome within a solution run.
/// </summary>
public sealed class SolutionProjectPublishResult
{
    public required DeployableProject Project { get; init; }

    public bool IsSuccess { get; init; }

    public bool IsDeploymentReady { get; init; }

    public string? PackagePath { get; init; }

    public string? ReleaseLibraryPath { get; init; }

    public string? LogPath { get; init; }

    public PendingPublishValidation? PendingValidation { get; init; }

    public string? ErrorMessage { get; init; }

    public TimeSpan Duration { get; init; }

    public PublishingResult? PublishingResult { get; init; }
}

/// <summary>
/// Live progress for solution-level publishing.
/// </summary>
public sealed class SolutionPublishingProgress
{
    public required string Phase { get; init; }

    public string Message { get; init; } = string.Empty;

    public int CompletedProjects { get; init; }

    public int TotalProjects { get; init; }

    public SolutionPublishProjectStatus? ProjectStatus { get; init; }
}

/// <summary>
/// Dashboard row for a single project during solution publishing.
/// </summary>
public sealed class SolutionPublishProjectStatus
{
    public required string ProjectName { get; init; }

    public required string ServiceId { get; init; }

    public required string CurrentStep { get; init; }

    public bool BuildSucceeded { get; init; }

    public bool DockerSucceeded { get; init; }

    public bool PackageSucceeded { get; init; }

    public bool ValidationSucceeded { get; init; }

    public bool DeploymentReady { get; init; }

    public bool Failed { get; init; }

    public int PercentComplete { get; init; }

    public TimeSpan Duration { get; init; }

    public string? ErrorMessage { get; init; }

    public string? PackagePath { get; init; }
}
