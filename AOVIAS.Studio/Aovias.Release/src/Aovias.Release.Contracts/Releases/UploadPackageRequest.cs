namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Request to upload a package to the runtime release root.
/// </summary>
public sealed class UploadPackageRequest
{
    public required string PackagePath { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }

    public required string UploadedBy { get; init; }
}
