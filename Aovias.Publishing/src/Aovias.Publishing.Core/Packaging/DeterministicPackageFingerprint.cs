using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aovias.Deployment.Contracts;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Computes deterministic fingerprints for deployment packages using <see cref="FingerprintPolicy"/>.
/// </summary>
public static class DeterministicPackageFingerprint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Regex TimestampPattern = new(
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Compute(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var dockerImageIdentity = ResolveDockerImageIdentity(archive);
        return ComputeArchive(archive, dockerImageIdentity);
    }

    public static string ComputeArchive(ZipArchive archive, string? dockerImageIdentity = null)
    {
        dockerImageIdentity ??= ResolveDockerImageIdentity(archive);
        using var composite = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var builder = new StringBuilder();

        foreach (var entry in archive.Entries.OrderBy(item => item.FullName, StringComparer.Ordinal))
        {
            if (entry.FullName.EndsWith('/'))
            {
                continue;
            }

            var entryName = entry.Name;
            builder.Append(entry.FullName);
            builder.Append('|');

            if (FingerprintPolicy.VolatilePackageEntries.Contains(entryName))
            {
                builder.Append("volatile");
                builder.Append('|');
                continue;
            }

            if (FingerprintPolicy.UsesImageIdentity(entryName))
            {
                var identity = dockerImageIdentity ?? "missing-image-identity";
                builder.Append(FingerprintPolicy.IdentityMarker);
                builder.Append(identity);
                builder.Append('|');
                continue;
            }

            using var stream = entry.Open();
            if (IsTextNormalizedEntry(entryName))
            {
                builder.Append(HashNormalizedText(entryName, stream));
            }
            else
            {
                builder.Append(PublishingContentHasher.HashStream(stream));
            }

            builder.Append('|');
        }

        composite.AppendData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(composite.GetHashAndReset()).ToLowerInvariant();
    }

    private static string? ResolveDockerImageIdentity(ZipArchive archive)
    {
        var manifestEntry = archive.GetEntry(DeployPackageLayout.ManifestFile);
        if (manifestEntry is null)
        {
            return null;
        }

        using var stream = manifestEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var json = reader.ReadToEnd();
        try
        {
            var manifest = JsonSerializer.Deserialize<DeployManifest>(json, JsonOptions);
            return manifest?.DockerImageIdentity;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsTextNormalizedEntry(string entryName)
        => FingerprintPolicy.TextNormalizedEntries.Contains(entryName)
           || entryName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
           || entryName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
           || entryName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
           || entryName.EndsWith(".template", StringComparison.OrdinalIgnoreCase);

    private static string HashNormalizedText(string entryName, Stream source)
    {
        using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var content = reader.ReadToEnd();

        if (entryName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            content = NormalizeJson(content, entryName);
        }
        else if (entryName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                 || entryName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                 || entryName.EndsWith(".template", StringComparison.OrdinalIgnoreCase))
        {
            content = TimestampPattern.Replace(content, "TIMESTAMP");
        }

        return PublishingContentHasher.HashText(content);
    }

    private static string NormalizeJson(string content, string entryName)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (FingerprintPolicy.IsContractFile(entryName))
            {
                return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = false });
            }

            return TimestampPattern.Replace(content, "TIMESTAMP");
        }
        catch (JsonException)
        {
            return TimestampPattern.Replace(content, "TIMESTAMP");
        }
    }
}
