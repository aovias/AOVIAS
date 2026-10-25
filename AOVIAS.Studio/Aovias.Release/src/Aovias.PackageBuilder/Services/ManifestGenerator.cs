using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Generates manifest.json metadata for package builds.
/// </summary>
public sealed class ManifestGenerator : Abstractions.IManifestGenerator
{
    /// <inheritdoc />
    public PackageManifest Generate(PackageBuildRequest request, string packageId, string packageHash)
    {
        return new PackageManifest
        {
            PackageId = packageId,
            PackageVersion = request.PackageVersion,
            ApplicationName = request.ApplicationName,
            ServiceId = request.ServiceId,
            DisplayName = request.DisplayName,
            RuntimeVersion = request.RuntimeVersion,
            PlatformVersion = request.PlatformVersion,
            Framework = request.Framework,
            BuildNumber = request.BuildNumber,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = request.CreatedBy,
            PackageHash = packageHash,
            Dependencies = request.Dependencies,
            MinimumRuntimeVersion = request.MinimumRuntimeVersion,
            SupportedOperatingSystems = request.SupportedOperatingSystems
        };
    }
}
