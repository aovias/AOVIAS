using System.Text.Json;
using Aovias.Publishing.Contracts.ReleaseLibrary;

namespace Aovias.Publishing.Core.ReleaseLibrary;

/// <summary>
/// Loads and atomically persists the Release Index document (Sprint M1.5 D4).
/// The index is the authoritative record; the release-library filesystem layout
/// is a storage detail underneath it, not a source of truth.
/// </summary>
public static class ReleaseIndexStore
{
    private const string IndexDirectoryName = ".release-index";
    private const string IndexFileName = "release-index.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetIndexDirectory(string releaseLibraryRoot)
        => Path.Combine(releaseLibraryRoot, IndexDirectoryName);

    public static string GetIndexPath(string releaseLibraryRoot)
        => Path.Combine(GetIndexDirectory(releaseLibraryRoot), IndexFileName);

    /// <summary>
    /// Loads the index document. An index that does not yet exist is not an
    /// error (a fresh, empty document is returned); an index that exists but
    /// fails to parse IS an error (Sprint M1.5 D10: "corrupted index"), because a
    /// release library that can no longer prove what it contains must never be
    /// silently treated as empty.
    /// </summary>
    public static ReleaseIndexDocument Load(string releaseLibraryRoot)
    {
        var path = GetIndexPath(releaseLibraryRoot);
        if (!File.Exists(path))
        {
            return new ReleaseIndexDocument();
        }

        string content;
        try
        {
            content = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Release Library index at '{path}' could not be read: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException($"Release Library index at '{path}' is empty. The index is corrupted or was truncated.");
        }

        ReleaseIndexDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<ReleaseIndexDocument>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Release Library index at '{path}' is corrupted (invalid JSON): {ex.Message}", ex);
        }

        if (document is null)
        {
            throw new InvalidOperationException($"Release Library index at '{path}' deserialized to null. The index is corrupted.");
        }

        var duplicateIds = document.Artifacts
            .GroupBy(artifact => artifact.ArtifactId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Release Library index at '{path}' is corrupted: duplicate artifact IDs present ({string.Join(", ", duplicateIds)}).");
        }

        return document;
    }

    /// <summary>
    /// Atomically persists the index: write to a temp file in the same directory,
    /// then move it into place, so a crash mid-write can never leave a partially
    /// written (and therefore corrupt) index on disk.
    /// </summary>
    public static void Save(string releaseLibraryRoot, ReleaseIndexDocument document)
    {
        var directory = GetIndexDirectory(releaseLibraryRoot);
        Directory.CreateDirectory(directory);

        var path = GetIndexPath(releaseLibraryRoot);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

        var toSave = document with { LastUpdatedUtc = DateTimeOffset.UtcNow };
        File.WriteAllText(tempPath, JsonSerializer.Serialize(toSave, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }
}
