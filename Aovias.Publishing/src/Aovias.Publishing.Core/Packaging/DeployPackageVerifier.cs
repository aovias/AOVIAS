using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Aovias.Publishing.Contracts;
using Aovias.Deployment.Contracts.Reading;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Verifies a generated <c>.aovdeploy</c> package immediately after creation.
/// Unpacks the archive and asserts that the manifest, checksums, compose, image,
/// metadata, config template, health, environment file and (optionally) the
/// deployment certificate are all present and consistent.
/// </summary>
public sealed class DeployPackageVerifier : IDeployPackageVerifier
{
    private readonly IDeployPackageReader packageReader;
    private readonly IDeploymentPackageContractReader contractReader;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DeployPackageVerifier(IDeployPackageReader packageReader, IDeploymentPackageContractReader contractReader)
    {
        this.packageReader = packageReader;
        this.contractReader = contractReader;
    }

    /// <inheritdoc />
    public async Task<DeployPackageVerificationResult> VerifyAsync(
        string packagePath,
        bool expectCertificate,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var verifiedFiles = new List<string>();

        if (!File.Exists(packagePath))
        {
            return DeployPackageVerificationResult.Failure([$"Package file was not found: {packagePath}"]);
        }

        if (!packagePath.EndsWith(DeployPackageLayout.Extension, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Package extension must be .aovdeploy.");
        }

        var checksumCount = 0;
        var hasCertificate = false;

        using (var archive = ZipFile.OpenRead(packagePath))
        {
            foreach (var required in DeployPackageLayout.RequiredFiles)
            {
                if (archive.GetEntry(required) is null)
                {
                    errors.Add($"Missing required file: {required}");
                }
                else
                {
                    verifiedFiles.Add(required);
                }
            }

            hasCertificate = archive.GetEntry(DeployPackageLayout.DeploymentReadinessCertificateFile) is not null;
            if (expectCertificate && !hasCertificate)
            {
                errors.Add($"Missing deployment certificate: {DeployPackageLayout.DeploymentReadinessCertificateFile}");
            }

            checksumCount = VerifyChecksums(archive, errors);
        }

        try
        {
            _ = contractReader.ReadFromPackage(packagePath);
            verifiedFiles.Add(Aovias.Deployment.Contracts.DeploymentPackageLayout.InfrastructureContractFile);
        }
        catch (DeploymentPackageContractException exception)
        {
            errors.Add(exception.Message);
        }

        var contents = await packageReader.ReadAsync(packagePath, cancellationToken);
        if (contents is null)
        {
            errors.Add("Unable to parse deployment package contents.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(contents.Manifest.Service))
            {
                errors.Add("Manifest service name is required.");
            }

            if (string.IsNullOrWhiteSpace(contents.Metadata.Publisher))
            {
                errors.Add("Metadata publisher is required.");
            }

            if (string.IsNullOrWhiteSpace(contents.Health.Endpoint))
            {
                errors.Add("Health endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(contents.ComposeContent))
            {
                errors.Add("docker-compose.yml is empty.");
            }

            if (string.IsNullOrWhiteSpace(contents.EnvTemplateContent))
            {
                errors.Add(".env.template is empty.");
            }

            if (string.IsNullOrWhiteSpace(contents.ConfigurationTemplateContent))
            {
                errors.Add("config-template.json is empty.");
            }

            if (!contents.HasServiceImage)
            {
                errors.Add("service-image.tar is missing or empty.");
            }

            if (expectCertificate)
            {
                if (contents.ReadinessCertificate is null)
                {
                    errors.Add("Deployment certificate could not be parsed.");
                }
                else if (string.IsNullOrWhiteSpace(contents.ReadinessCertificate.PackageChecksum))
                {
                    errors.Add("Deployment certificate is missing the package checksum.");
                }
            }
        }

        return errors.Count == 0
            ? DeployPackageVerificationResult.Success(verifiedFiles, checksumCount, hasCertificate)
            : DeployPackageVerificationResult.Failure(errors);
    }

    private static int VerifyChecksums(ZipArchive archive, List<string> errors)
    {
        var checksumEntry = archive.GetEntry(DeployPackageLayout.ChecksumsFile);
        if (checksumEntry is null)
        {
            errors.Add("Missing checksums file.");
            return 0;
        }

        Dictionary<string, string>? checksums;
        using (var stream = checksumEntry.Open())
        {
            checksums = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, JsonOptions);
        }

        if (checksums is null || checksums.Count == 0)
        {
            errors.Add("Checksums file is empty or invalid.");
            return 0;
        }

        foreach (var (name, expected) in checksums)
        {
            var entry = archive.GetEntry(name);
            if (entry is null)
            {
                errors.Add($"Checksum references missing file: {name}");
                continue;
            }

            using var entryStream = entry.Open();
            var actual = PublishingContentHasher.HashStream(entryStream);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Checksum mismatch for {name}.");
            }
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/'))
            {
                continue;
            }

            if (string.Equals(entry.Name, DeployPackageLayout.ChecksumsFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!checksums.ContainsKey(entry.Name))
            {
                errors.Add($"Orphan package entry not covered by checksums: {entry.Name}");
            }
        }

        return checksums.Count;
    }
}
