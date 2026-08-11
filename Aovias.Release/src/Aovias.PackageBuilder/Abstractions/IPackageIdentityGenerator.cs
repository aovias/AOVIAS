namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Generates immutable package identifiers.
/// </summary>
public interface IPackageIdentityGenerator
{
    /// <summary>
    /// Creates a new package identifier.
    /// </summary>
    string GeneratePackageId();
}
