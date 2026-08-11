using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Verifies deployment packages, contract integrity, and deterministic fingerprints.
/// </summary>
public sealed class PackageVerifier : IPackageVerifier
{
    private readonly IDeployPackageVerifier deployPackageVerifier;
    private readonly IDeployPackageReader packageReader;
    private readonly IGoldenPackageRepository goldenPackageRepository;
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public PackageVerifier(
        IDeployPackageVerifier deployPackageVerifier,
        IDeployPackageReader packageReader,
        IGoldenPackageRepository goldenPackageRepository,
        IOptionsMonitor<PublishingSettings> settings)
    {
        this.deployPackageVerifier = deployPackageVerifier;
        this.packageReader = packageReader;
        this.goldenPackageRepository = goldenPackageRepository;
        this.settings = settings;
    }

    public async Task<PackageVerificationReport> VerifyAsync(
        string packagePath,
        bool expectCertificate,
        bool expectDeterministicFingerprint,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var baseResult = await deployPackageVerifier.VerifyAsync(packagePath, expectCertificate, cancellationToken);
        if (!baseResult.IsValid)
        {
            errors.AddRange(baseResult.Errors);
        }

        var contents = await packageReader.ReadAsync(packagePath, cancellationToken);
        if (contents is null)
        {
            errors.Add("Unable to parse deployment package for extended verification.");
        }
        else
        {
            errors.AddRange(VerifyContracts(contents));
        }

        string? fingerprint = null;
        if (expectDeterministicFingerprint)
        {
            try
            {
                fingerprint = DeterministicPackageFingerprint.Compute(packagePath);
                if (contents?.Metadata is not null
                    && !string.IsNullOrWhiteSpace(contents.Metadata.PublishingInputFingerprint)
                    && !string.IsNullOrWhiteSpace(contents.Metadata.DockerImageIdentity))
                {
                    errors.AddRange(ValidateBuildManifestConsistency(packagePath, contents, fingerprint));
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Deterministic fingerprint computation failed: {exception.Message}");
            }
        }

        var certified = baseResult.IsValid
            && errors.Count == 0
            && expectCertificate
            && baseResult.HasCertificate
            && contents?.DeploymentContracts is not null;

        if (errors.Count > 0)
        {
            return PackageVerificationReport.Failure(errors);
        }

        return PackageVerificationReport.Success(
            baseResult.VerifiedFiles,
            baseResult.ChecksumCount,
            baseResult.HasCertificate,
            fingerprint,
            certified);
    }

    public async Task<GoldenPackageComparisonResult> CompareToGoldenAsync(
        string packagePath,
        string serviceId,
        CancellationToken cancellationToken = default)
    {
        var fingerprint = DeterministicPackageFingerprint.Compute(packagePath);
        return await goldenPackageRepository.CompareAsync(serviceId, packagePath, fingerprint, cancellationToken);
    }

    private static IEnumerable<string> VerifyContracts(DeployPackageContents contents)
    {
        var errors = new List<string>();
        if (contents.DeploymentContracts is null)
        {
            errors.Add("Deployment contracts are missing from the package.");
            return errors;
        }

        var report = Aovias.Deployment.Contracts.Validation.ContractValidator.Validate(contents.DeploymentContracts);
        foreach (var item in report.Results.Where(result => !result.Passed))
        {
            errors.AddRange(item.Errors.Select(error => $"{item.Name}: {error}"));
        }

        foreach (var contractFile in DeploymentPackageLayout.ContractFiles)
        {
            if (!contents.Checksums.ContainsKey(contractFile))
            {
                errors.Add($"Contract checksum missing for {contractFile}.");
            }
        }

        return errors;
    }

    private static IEnumerable<string> ValidateBuildManifestConsistency(
        string packagePath,
        DeployPackageContents contents,
        string packageFingerprint)
    {
        var errors = new List<string>();
        using var archive = System.IO.Compression.ZipFile.OpenRead(packagePath);
        var entry = archive.GetEntry(DeployPackageLayout.PublishingBuildManifestFile);
        if (entry is null)
        {
            errors.Add($"Missing publishing build manifest: {DeployPackageLayout.PublishingBuildManifestFile}.");
            return errors;
        }

        using var stream = entry.Open();
        var manifest = System.Text.Json.JsonSerializer.Deserialize<PublishingBuildManifest>(
            stream,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (manifest is null)
        {
            errors.Add("Publishing build manifest could not be parsed.");
            return errors;
        }

        if (!string.Equals(manifest.PublishingInputFingerprint, contents.Metadata.PublishingInputFingerprint, StringComparison.Ordinal))
        {
            errors.Add("Publishing build manifest input fingerprint does not match metadata.json.");
        }

        if (!string.Equals(manifest.DockerImageIdentity, contents.Metadata.DockerImageIdentity, StringComparison.Ordinal))
        {
            errors.Add("Publishing build manifest docker image identity does not match metadata.json.");
        }

        if (!string.Equals(manifest.SourceRevision, contents.Metadata.SourceRevision, StringComparison.Ordinal))
        {
            errors.Add("Publishing build manifest source revision does not match metadata.json.");
        }

        if (!string.IsNullOrWhiteSpace(manifest.PackageFingerprint)
            && !string.Equals(manifest.PackageFingerprint, packageFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Publishing build manifest package fingerprint is stale.");
        }

        return errors;
    }
}
