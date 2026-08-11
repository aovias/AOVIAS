using System.Security.Cryptography;
using System.Text;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Generates immutable PKG-* identifiers.
/// </summary>
public sealed class PackageIdentityGenerator : Abstractions.IPackageIdentityGenerator
{
    /// <inheritdoc />
    public string GeneratePackageId()
        => $"PKG-{RandomNumberGenerator.GetHexString(4, true).ToUpperInvariant()}";
}
