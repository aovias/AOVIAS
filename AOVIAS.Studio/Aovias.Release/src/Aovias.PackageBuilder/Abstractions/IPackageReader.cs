using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Reads structured contents from an AOVIAS .aovpkg archive.
/// </summary>
public interface IPackageReader
{
    /// <summary>
    /// Reads and parses a package archive.
    /// </summary>
    Task<PackageContents> ReadAsync(string packagePath, CancellationToken cancellationToken = default);
}
