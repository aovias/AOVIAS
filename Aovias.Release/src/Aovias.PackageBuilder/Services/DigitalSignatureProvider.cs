namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Architecture-only digital signature provider reserved for future signing integration.
/// </summary>
public sealed class DigitalSignatureProvider : Abstractions.IPackageSigner
{
    /// <inheritdoc />
    public Task SignAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signaturePath = Path.ChangeExtension(packagePath, ".sig");
        var placeholder = $"UNSIGNED::{Path.GetFileName(packagePath)}::{DateTimeOffset.UtcNow:O}";
        return File.WriteAllTextAsync(signaturePath, placeholder, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> VerifyAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var signaturePath = Path.ChangeExtension(packagePath, ".sig");
        if (!File.Exists(signaturePath))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(true);
    }
}
