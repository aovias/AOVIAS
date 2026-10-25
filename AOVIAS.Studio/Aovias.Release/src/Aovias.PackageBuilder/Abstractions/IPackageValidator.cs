using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Validates AOVIAS package structure, checksums, and compatibility metadata.
/// </summary>
public interface IPackageValidator
{
    /// <summary>
    /// Validates a package archive.
    /// </summary>
    Task<PackageValidationResult> ValidateAsync(string packagePath, CancellationToken cancellationToken = default);
}
