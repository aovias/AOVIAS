namespace Aovias.PackageBuilder.Abstractions;

/// <summary>
/// Computes and verifies SHA-256 checksums for package artifacts.
/// </summary>
public interface IChecksumProvider
{
    /// <summary>
    /// Computes the SHA-256 hash of a file.
    /// </summary>
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the SHA-256 hash of a stream.
    /// </summary>
    Task<string> ComputeStreamHashAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates checksum file content for the supplied relative paths.
    /// </summary>
    Task<string> GenerateChecksumFileContentAsync(
        string rootDirectory,
        IReadOnlyList<string> relativePaths,
        CancellationToken cancellationToken = default);
}
