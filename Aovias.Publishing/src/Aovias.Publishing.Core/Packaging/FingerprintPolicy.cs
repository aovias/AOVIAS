using Aovias.Deployment.Contracts;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Versioned deterministic fingerprint policy for deployment packages (M1.2 / spec N2).
/// </summary>
public static class FingerprintPolicy
{
    public const int Version = 1;

    public const string IdentityMarker = "identity:";

    /// <summary>
    /// Package entries excluded from content hashing (timestamps, certificates, reports).
    /// </summary>
    public static readonly IReadOnlySet<string> VolatilePackageEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        DeployPackageLayout.MetadataFile,
        DeployPackageLayout.DeploymentReadinessCertificateFile,
        DeployPackageLayout.PublishingReportFile,
        DeployPackageLayout.PublishingBuildManifestFile
    };

    /// <summary>
    /// JSON/text entries normalized by timestamp replacement before hashing.
    /// </summary>
    public static readonly IReadOnlySet<string> TextNormalizedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        DeployPackageLayout.ComposeFile,
        DeployPackageLayout.EnvTemplateFile,
        DeployPackageLayout.ConfigTemplateFile,
        DeployPackageLayout.HealthFile,
        DeployPackageLayout.ManifestFile
    };

    public static bool IsContractFile(string entryName)
        => DeploymentPackageLayout.ContractFiles.Contains(entryName)
           || string.Equals(entryName, DeployPackageLayout.RuntimeCapabilitiesFile, StringComparison.OrdinalIgnoreCase);

    public static bool UsesImageIdentity(string entryName)
        => string.Equals(entryName, DeployPackageLayout.ServiceImageTar, StringComparison.OrdinalIgnoreCase);
}
