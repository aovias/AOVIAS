namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// Request to build a new AOVIAS package from published output.
/// </summary>
public sealed class PackageBuildRequest
{
    public string? PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string ApplicationName { get; init; }

    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    public required string PublishedOutputPath { get; init; }

    public required string OutputDirectory { get; init; }

    public required string CreatedBy { get; init; }

    public required string RuntimeVersion { get; init; }

    public required string PlatformVersion { get; init; }

    public required string Framework { get; init; }

    public required string BuildNumber { get; init; }

    public required string MinimumRuntimeVersion { get; init; }

    public IReadOnlyList<string> SupportedOperatingSystems { get; init; } = [];

    public IReadOnlyList<PackageDependency> Dependencies { get; init; } = [];

    public ServiceDescription? Service { get; init; }

    public DeploymentDescription? Deployment { get; init; }

    public HealthDescription? Health { get; init; }

    public string? ReleaseNotes { get; init; }
}
