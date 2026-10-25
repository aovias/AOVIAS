using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Generates manifest.json content for AOVIAS packages.
/// </summary>
public interface IManifestGenerator
{
    /// <summary>
    /// Creates a manifest for a package build request.
    /// </summary>
    PackageManifest Generate(PackageBuildRequest request, string packageId, string packageHash);
}
