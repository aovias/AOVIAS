using System.IO.Compression;
using System.Text;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Packaging;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class DeterministicPackageFingerprintTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "fingerprint-" + Guid.NewGuid().ToString("N"));

    public DeterministicPackageFingerprintTests()
    {
        Directory.CreateDirectory(tempDirectory);
    }

    [Fact]
    public void Compute_IdenticalPackages_ProduceSameFingerprint()
    {
        const string imageIdentity = "sha256:abc123identity";
        var first = CreateSamplePackage("service-a", "1.0.0", imageIdentity);
        var second = CreateSamplePackage("service-a", "1.0.0", imageIdentity);

        Assert.Equal(
            DeterministicPackageFingerprint.Compute(first),
            DeterministicPackageFingerprint.Compute(second));
    }

    [Fact]
    public void Compute_DifferentCompose_ProducesDifferentFingerprint()
    {
        const string imageIdentity = "sha256:abc123identity";
        var baseline = CreateSamplePackage("service-a", "1.0.0", imageIdentity, composeSuffix: "baseline");
        var changed = CreateSamplePackage("service-a", "1.0.0", imageIdentity, composeSuffix: "changed");

        Assert.NotEqual(
            DeterministicPackageFingerprint.Compute(baseline),
            DeterministicPackageFingerprint.Compute(changed));
    }

    [Fact]
    public void Compute_DifferentImageIdentity_ProducesDifferentFingerprint()
    {
        var first = CreateSamplePackage("service-a", "1.0.0", "sha256:first");
        var second = CreateSamplePackage("service-a", "1.0.0", "sha256:second");

        Assert.NotEqual(
            DeterministicPackageFingerprint.Compute(first),
            DeterministicPackageFingerprint.Compute(second));
    }

    [Fact]
    public void Compute_VolatileMetadata_DoesNotAffectFingerprint()
    {
        const string imageIdentity = "sha256:abc123identity";
        var firstTimestamp = CreateSamplePackage("service-a", "1.0.0", imageIdentity, metadataCreatedUtc: "2026-01-01T00:00:00Z");
        var secondTimestamp = CreateSamplePackage("service-a", "1.0.0", imageIdentity, metadataCreatedUtc: "2026-01-02T00:00:00Z");

        Assert.Equal(
            DeterministicPackageFingerprint.Compute(firstTimestamp),
            DeterministicPackageFingerprint.Compute(secondTimestamp));
    }

    [Fact]
    public void Compute_BinaryTarUsesImageIdentity_NotTarBytes()
    {
        const string imageIdentity = "sha256:abc123identity";
        var first = CreateSamplePackage("service-a", "1.0.0", imageIdentity, tarContent: [1, 2, 3]);
        var second = CreateSamplePackage("service-a", "1.0.0", imageIdentity, tarContent: [9, 8, 7]);

        Assert.Equal(
            DeterministicPackageFingerprint.Compute(first),
            DeterministicPackageFingerprint.Compute(second));
    }

    private string CreateSamplePackage(
        string service,
        string version,
        string dockerImageIdentity,
        string? metadataCreatedUtc = null,
        string composeSuffix = "baseline",
        byte[]? tarContent = null)
    {
        var packagePath = Path.Combine(tempDirectory, $"{service}-{composeSuffix}-{Guid.NewGuid():N}.aovdeploy");
        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        tarContent ??= [0x1F, 0x8B, 0x08, 0x00];

        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        {
            WriteEntry(
                archive,
                DeployPackageLayout.ManifestFile,
                $$"""{"service":"{{service}}","version":"{{version}}","dockerImageIdentity":"{{dockerImageIdentity}}","sourceRevision":"rev","publishingInputFingerprint":"input"}""");
            WriteEntry(archive, DeployPackageLayout.ComposeFile, $"services:\n  app:\n    image: aovias/{service}:{version}-{composeSuffix}\n");
            WriteEntry(archive, DeployPackageLayout.HealthFile, """{"endpoint":"/health"}""");
            WriteEntry(archive, DeployPackageLayout.ConfigTemplateFile, "{}");
            WriteEntry(archive, DeployPackageLayout.EnvTemplateFile, "AOVIAS_CONFIG_ROOT=/opt/aovias/runtime/config\n");
            WriteEntry(archive, DeployPackageLayout.ChecksumsFile, "{}");
            WriteEntry(archive, DeployPackageLayout.ServiceImageTar, tarContent);
            WriteEntry(
                archive,
                DeployPackageLayout.PublishingBuildManifestFile,
                $$"""{"schemaVersion":1,"sourceRevision":"rev","buildUtc":"2026-01-01T00:00:00Z","buildVersion":"{{version}}","publishingInputFingerprint":"input","dockerImageIdentity":"{{dockerImageIdentity}}","publishingEngineVersion":"1.0.0","fingerprintPolicyVersion":1,"profile":"demo"}""");

            WriteEntry(
                archive,
                DeployPackageLayout.MetadataFile,
                $$"""{"createdUtc":"{{metadataCreatedUtc ?? "2026-01-01T00:00:00Z"}}","publisher":"test","publishingEngineVersion":"1.0.0","packageVersion":"1.0.0","packageType":".aovdeploy"}""");
        }

        return packagePath;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
        => WriteEntry(archive, name, Encoding.UTF8.GetBytes(content));

    private static void WriteEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(content);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
