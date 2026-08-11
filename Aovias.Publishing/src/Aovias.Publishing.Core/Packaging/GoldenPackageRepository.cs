using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Stores verified golden packages for regression comparison.
/// </summary>
public sealed class GoldenPackageRepository : IGoldenPackageRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IOptionsMonitor<PublishingSettings> settings;

    public GoldenPackageRepository(IOptionsMonitor<PublishingSettings> settings)
    {
        this.settings = settings;
    }

    public async Task StoreGoldenAsync(
        string serviceId,
        string packagePath,
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        var serviceDirectory = GetServiceDirectory(serviceId);
        Directory.CreateDirectory(serviceDirectory);

        var goldenPackagePath = Path.Combine(serviceDirectory, $"{serviceId}.golden.aovdeploy");
        File.Copy(packagePath, goldenPackagePath, overwrite: true);

        var metadata = new GoldenPackageMetadata
        {
            ServiceId = serviceId,
            PackagePath = goldenPackagePath,
            Fingerprint = fingerprint,
            StoredUtc = DateTimeOffset.UtcNow
        };

        await File.WriteAllTextAsync(
            Path.Combine(serviceDirectory, "golden-metadata.json"),
            JsonSerializer.Serialize(metadata, JsonOptions),
            cancellationToken);
    }

    public Task<GoldenPackageComparisonResult> CompareAsync(
        string serviceId,
        string packagePath,
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var goldenPath = GetGoldenPackagePath(serviceId);
        if (string.IsNullOrWhiteSpace(goldenPath) || !File.Exists(goldenPath))
        {
            return Task.FromResult(new GoldenPackageComparisonResult
            {
                Matches = true,
                RequiresApproval = false,
                CandidatePackagePath = packagePath,
                GoldenPackagePath = goldenPath
            });
        }

        var metadataPath = Path.Combine(GetServiceDirectory(serviceId), "golden-metadata.json");
        if (!File.Exists(metadataPath))
        {
            return Task.FromResult(new GoldenPackageComparisonResult
            {
                Matches = false,
                RequiresApproval = true,
                CandidatePackagePath = packagePath,
                GoldenPackagePath = goldenPath,
                Differences = ["Golden metadata is missing."]
            });
        }

        var metadata = JsonSerializer.Deserialize<GoldenPackageMetadata>(File.ReadAllText(metadataPath), JsonOptions);
        if (metadata is null || !string.Equals(metadata.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new GoldenPackageComparisonResult
            {
                Matches = false,
                RequiresApproval = true,
                CandidatePackagePath = packagePath,
                GoldenPackagePath = goldenPath,
                Differences =
                [
                    $"Deterministic fingerprint changed. Expected {metadata?.Fingerprint ?? "(none)"}, actual {fingerprint}."
                ]
            });
        }

        return Task.FromResult(new GoldenPackageComparisonResult
        {
            Matches = true,
            RequiresApproval = false,
            CandidatePackagePath = packagePath,
            GoldenPackagePath = goldenPath
        });
    }

    public string? GetGoldenPackagePath(string serviceId)
    {
        var path = Path.Combine(GetServiceDirectory(serviceId), $"{serviceId}.golden.aovdeploy");
        return File.Exists(path) ? path : null;
    }

    private string GetServiceDirectory(string serviceId)
        => Path.Combine(settings.CurrentValue.ArtifactRoot, "golden-packages", serviceId);

    private sealed class GoldenPackageMetadata
    {
        public required string ServiceId { get; init; }

        public required string PackagePath { get; init; }

        public required string Fingerprint { get; init; }

        public required DateTimeOffset StoredUtc { get; init; }
    }
}
