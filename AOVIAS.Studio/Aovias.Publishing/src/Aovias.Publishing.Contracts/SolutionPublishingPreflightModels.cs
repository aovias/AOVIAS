namespace Aovias.Publishing.Contracts;

/// <summary>
/// Verifies publishing prerequisites before a solution publish run.
/// </summary>
public interface ISolutionPublishingPreflight
{
    Task<SolutionPublishingPreflightResult> RunAsync(
        SolutionPublishingPreflightRequest request,
        IProgress<SolutionPublishingPreflightProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Inspects generated release-library output after publishing.
/// </summary>
public interface ISolutionPackageInspector
{
    Task<SolutionPackageInspectionResult> InspectReleaseLibraryAsync(
        string releaseLibraryRoot,
        string? profile = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Preflight request mirroring solution publish parameters.
/// </summary>
public sealed class SolutionPublishingPreflightRequest
{
    public string? PlatformRoot { get; init; }

    public string Profile { get; init; } = "demo";

    public string Version { get; init; } = "1.0.0";

    public string? OutputDirectory { get; init; }

    public bool SkipSolutionBuild { get; init; }

    public bool SkipDockerBuildTest { get; init; }

    public bool SkipNpmRestore { get; init; }

    public string? DeploymentValidationMode { get; init; }
}

/// <summary>
/// Outcome of preflight verification.
/// </summary>
public sealed class SolutionPublishingPreflightResult
{
    public bool IsReady { get; init; }

    public string Verdict { get; init; } = "BLOCKED";

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<SolutionPreflightCheckResult> Checks { get; init; } = [];

    public IReadOnlyList<string> BlockingReasons { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyDictionary<string, int> ProjectWarningCounts { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public long EstimatedRequiredBytes { get; init; }

    public long AvailableBytes { get; init; }

    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Individual preflight category result.
/// </summary>
public sealed class SolutionPreflightCheckResult
{
    public required string Category { get; init; }

    public bool Passed { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Details { get; init; } = [];
}

/// <summary>
/// Live preflight progress.
/// </summary>
public sealed class SolutionPublishingPreflightProgress
{
    public required string Category { get; init; }

    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Post-publish release library inspection.
/// </summary>
public sealed class SolutionPackageInspectionResult
{
    public bool IsValid { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<SolutionPackageInspectionEntry> Packages { get; init; } = [];

    public string? SolutionDeploymentManifestPath { get; init; }

    public string? DemoDeploymentBundlePath { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Per-package inspection within the release library.
/// </summary>
public sealed class SolutionPackageInspectionEntry
{
    public required string ServiceId { get; init; }

    public required string Category { get; init; }

    public required string PackagePath { get; init; }

    public bool IsValid { get; init; }

    public IReadOnlyList<string> PresentFiles { get; init; } = [];

    public IReadOnlyList<string> MissingFiles { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];
}
