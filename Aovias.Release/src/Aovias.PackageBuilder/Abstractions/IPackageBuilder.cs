using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Builds and validates AOVIAS .aovpkg release packages.
/// </summary>
public interface IPackageBuilder
{
    /// <summary>
    /// Creates a new .aovpkg archive from published output.
    /// </summary>
    Task<PackageBuildResult> CreatePackageAsync(PackageBuildRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an existing .aovpkg archive.
    /// </summary>
    Task<PackageValidationResult> ValidatePackageAsync(string packagePath, CancellationToken cancellationToken = default);
}
