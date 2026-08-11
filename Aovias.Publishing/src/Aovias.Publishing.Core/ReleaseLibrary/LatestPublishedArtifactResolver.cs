using Aovias.Publishing.Contracts.ReleaseLibrary;

namespace Aovias.Publishing.Core.ReleaseLibrary;

/// <summary>
/// Resolves the newest Release Library artifact whose package file exists on disk.
/// </summary>
public static class LatestPublishedArtifactResolver
{
    public static PublishedArtifactSelection? Resolve(
        string releaseLibraryRoot,
        IReadOnlyList<ReleaseArtifactRecord> records)
    {
        if (records.Count == 0)
        {
            return null;
        }

        var ordered = records.OrderByDescending(record => record.GeneratedUtc).ToList();
        var selected = SelectNewestWithPackage(releaseLibraryRoot, ordered.Where(record => record.IsCertified))
            ?? SelectNewestWithPackage(releaseLibraryRoot, ordered);

        return selected;
    }

    private static PublishedArtifactSelection? SelectNewestWithPackage(
        string releaseLibraryRoot,
        IEnumerable<ReleaseArtifactRecord> candidates)
    {
        foreach (var record in candidates)
        {
            var packagePath = Path.Combine(releaseLibraryRoot, record.RelativePackagePath);
            if (File.Exists(packagePath))
            {
                return new PublishedArtifactSelection
                {
                    Record = record,
                    PackagePath = packagePath
                };
            }
        }

        return null;
    }
}
