namespace Aovias.Release.Contracts;

/// <summary>
/// Well-known file names and paths within an AOVIAS package archive.
/// </summary>
public static class PackageLayout
{
    public const string Extension = ".aovpkg";

    public const string ManifestFile = "manifest.json";

    public const string ServiceFile = "service.json";

    public const string DeploymentFile = "deployment.json";

    public const string HealthFile = "health.json";

    public const string ChecksumsFile = "checksums.sha256";

    public const string ReleaseNotesFile = "release-notes.md";

    public const string SignatureFile = "signature.sig";

    public const string ArtifactsDirectory = "artifacts/";

    public const string LogsDirectory = "logs/";
}
