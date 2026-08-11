using System.Security.Cryptography;
using System.Text;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Streaming SHA-256 helpers for deterministic publishing fingerprints.
/// </summary>
public static class PublishingContentHasher
{
    public static string HashText(string? content)
        => HashBytes(Encoding.UTF8.GetBytes(content ?? string.Empty));

    public static string HashBytes(ReadOnlySpan<byte> bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static string HashStream(Stream stream)
    {
        using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            incremental.AppendData(buffer.AsSpan(0, read));
        }

        return Convert.ToHexString(incremental.GetHashAndReset()).ToLowerInvariant();
    }

    public static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return HashStream(stream);
    }

    public static string HashDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return HashText(string.Empty);
        }

        var builder = new StringBuilder();
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            builder.Append(relative);
            builder.Append('|');
            builder.Append(HashFile(file));
            builder.Append('|');
        }

        return HashText(builder.ToString());
    }

    public static string HashCanonicalJson(IReadOnlyDictionary<string, string> components)
    {
        var payload = string.Join(
            "|",
            components.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        return HashText(payload);
    }
}
