using System.Security.Cryptography;
using System.Text;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// SHA-256 checksum provider for package artifacts.
/// </summary>
public sealed class Sha256ChecksumProvider : Abstractions.IChecksumProvider
{
    /// <inheritdoc />
    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ComputeStreamHashAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ComputeStreamHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc />
    public async Task<string> GenerateChecksumFileContentAsync(
        string rootDirectory,
        IReadOnlyList<string> relativePaths,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();

        foreach (var relativePath in relativePaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(rootDirectory, relativePath);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var hash = await ComputeFileHashAsync(fullPath, cancellationToken);
            builder.Append(hash);
            builder.Append("  ");
            builder.AppendLine(relativePath.Replace('\\', '/'));
        }

        return builder.ToString();
    }
}
