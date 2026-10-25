namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Result of uploading a package.
/// </summary>
public sealed class UploadPackageResult
{
    public required string ReleaseId { get; init; }

    public required string PackageId { get; init; }

    public required string PackagePath { get; init; }

    public required string State { get; init; }
}
