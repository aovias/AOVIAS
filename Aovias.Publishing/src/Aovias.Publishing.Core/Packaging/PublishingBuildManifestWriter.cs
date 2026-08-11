using System.Text.Json;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Writes the publishing build manifest into the staging directory (M1.2 D7).
/// </summary>
public static class PublishingBuildManifestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Write(PublishingContext context, IReadOnlyDictionary<string, string>? artifactFingerprints = null)
    {
        var identity = context.BuildIdentity ?? throw new InvalidOperationException("Build identity is required.");
        var staging = context.StagingDirectory ?? throw new InvalidOperationException("Staging directory is required.");
        var manifest = PublishingBuildManifest.FromIdentity(identity, artifactFingerprints);
        File.WriteAllText(
            Path.Combine(staging, DeployPackageLayout.PublishingBuildManifestFile),
            JsonSerializer.Serialize(manifest, JsonOptions));
    }
}
