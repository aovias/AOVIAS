using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Core.ReleaseLibrary;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class LatestPublishedArtifactResolverTests
{
    [Fact]
    public void Resolve_prefers_certified_artifact_over_newer_uncertified_entry()
    {
        using var root = new TempReleaseLibraryRoot();
        var certifiedPath = root.WritePackage("certified.aovdeploy", "certified-payload");
        var uncertifiedPath = root.WritePackage("uncertified.aovdeploy", "uncertified-payload");

        var certified = BuildRecord("runtime-agent", "1.0.2", certifiedPath, certified: true, generatedUtc: DateTimeOffset.UtcNow.AddHours(-2));
        var uncertified = BuildRecord("runtime-agent", "1.0.3", uncertifiedPath, certified: false, generatedUtc: DateTimeOffset.UtcNow);

        var selection = LatestPublishedArtifactResolver.Resolve(root.Path, [uncertified, certified]);

        Assert.NotNull(selection);
        Assert.Equal("1.0.2", selection!.Record.Version);
        Assert.Equal(certifiedPath, selection.PackagePath);
    }

    [Fact]
    public void Resolve_falls_back_to_uncertified_when_no_certified_package_exists_on_disk()
    {
        using var root = new TempReleaseLibraryRoot();
        var packagePath = root.WritePackage("uncertified.aovdeploy", "uncertified-payload");
        var uncertified = BuildRecord("runtime-agent", "1.0.3", packagePath, certified: false);

        var selection = LatestPublishedArtifactResolver.Resolve(root.Path, [uncertified]);

        Assert.NotNull(selection);
        Assert.Equal("1.0.3", selection!.Record.Version);
    }

    [Fact]
    public void Resolve_skips_missing_package_files_and_selects_next_newest_present_entry()
    {
        using var root = new TempReleaseLibraryRoot();
        var presentPath = root.WritePackage("present.aovdeploy", "present-payload");
        var missing = BuildRecord("runtime-agent", "1.0.4", Path.Combine(root.Path, "missing", "missing.aovdeploy"), certified: true, generatedUtc: DateTimeOffset.UtcNow);
        var present = BuildRecord("runtime-agent", "1.0.3", presentPath, certified: true, generatedUtc: DateTimeOffset.UtcNow.AddHours(-1));

        var selection = LatestPublishedArtifactResolver.Resolve(root.Path, [missing, present]);

        Assert.NotNull(selection);
        Assert.Equal("1.0.3", selection!.Record.Version);
    }

    private static ReleaseArtifactRecord BuildRecord(
        string serviceId,
        string version,
        string absolutePackagePath,
        bool certified,
        DateTimeOffset? generatedUtc = null)
        => new()
        {
            ArtifactId = $"{serviceId}-{version}",
            ServiceId = serviceId,
            Service = serviceId,
            Version = version,
            BuildIdentity = "rev@build",
            SourceRevision = "rev",
            PublishingFingerprint = $"fp-{version}",
            PackageHash = "hash",
            PackageSizeBytes = 1,
            GeneratedUtc = generatedUtc ?? DateTimeOffset.UtcNow,
            ContractVersion = "1.0",
            PublishingEngineVersion = "1.0",
            SupportedRuntimeVersion = "1.0",
            DeploymentCompatibility = "1.0",
            RelativePackagePath = Path.GetFileName(absolutePackagePath),
            IsCertified = certified
        };

    private sealed class TempReleaseLibraryRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "latest-artifact-tests", Guid.NewGuid().ToString("N"));

        public TempReleaseLibraryRoot() => Directory.CreateDirectory(Path);

        public string WritePackage(string fileName, string payload)
        {
            var packagePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(packagePath, payload);
            return packagePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
