using System.Text.Json;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.Packages;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Reads structured metadata from .aovpkg archives.
/// </summary>
public sealed class PackageArchiveReader : Abstractions.IPackageReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    public async Task<PackageContents> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException("Package file was not found.", packagePath);
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "aovpkg-read", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(packagePath, tempDirectory, overwriteFiles: true);

            var manifest = await ReadJsonAsync<PackageManifest>(tempDirectory, PackageLayout.ManifestFile, cancellationToken);
            var service = await ReadJsonAsync<ServiceDescription>(tempDirectory, PackageLayout.ServiceFile, cancellationToken);
            var deployment = await ReadJsonAsync<DeploymentDescription>(tempDirectory, PackageLayout.DeploymentFile, cancellationToken);
            var health = await ReadJsonAsync<HealthDescription>(tempDirectory, PackageLayout.HealthFile, cancellationToken);
            var releaseNotesPath = Path.Combine(tempDirectory, PackageLayout.ReleaseNotesFile);
            var releaseNotes = File.Exists(releaseNotesPath) ? await File.ReadAllTextAsync(releaseNotesPath, cancellationToken) : string.Empty;
            var checksums = ReadChecksums(tempDirectory);
            var signaturePath = Path.Combine(tempDirectory, PackageLayout.SignatureFile);
            var signature = File.Exists(signaturePath) ? await File.ReadAllTextAsync(signaturePath, cancellationToken) : null;
            var artifactsDirectory = Path.Combine(tempDirectory, "artifacts");
            var artifactPaths = Directory.Exists(artifactsDirectory)
                ? Directory.GetFiles(artifactsDirectory, "*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(tempDirectory, path).Replace('\\', '/'))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : [];

            return new PackageContents
            {
                PackagePath = packagePath,
                Manifest = manifest,
                Service = service,
                Deployment = deployment,
                Health = health,
                ReleaseNotes = releaseNotes,
                Checksums = checksums,
                Signature = signature,
                ArtifactPaths = artifactPaths
            };
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup for temp extraction directory.
            }
        }
    }

    private static async Task<T> ReadJsonAsync<T>(string root, string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, fileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required package file '{fileName}' was not found.");
        }

        await using var stream = File.OpenRead(path);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        return value ?? throw new InvalidOperationException($"Failed to deserialize '{fileName}'.");
    }

    private static IReadOnlyDictionary<string, string> ReadChecksums(string root)
    {
        var checksumPath = Path.Combine(root, PackageLayout.ChecksumsFile);
        if (!File.Exists(checksumPath))
        {
            return new Dictionary<string, string>();
        }

        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(checksumPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split("  ", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                entries[parts[1]] = parts[0];
            }
        }

        return entries;
    }
}
