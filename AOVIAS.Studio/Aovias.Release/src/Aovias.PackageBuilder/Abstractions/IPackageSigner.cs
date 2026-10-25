namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Architecture hook for future digital signing of AOVIAS packages.
/// </summary>
public interface IPackageSigner
{
    /// <summary>
    /// Signs a package archive.
    /// </summary>
    Task SignAsync(string packagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a package signature when present.
    /// </summary>
    Task<bool> VerifyAsync(string packagePath, CancellationToken cancellationToken = default);
}
