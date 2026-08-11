using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Common;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Builds the final <c>.aovdeploy</c> ZIP archive.
/// </summary>
public sealed class DeployPackageBuilder : IDeployPackageBuilder
{
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly IDeploymentPackageContractReader contractReader;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public DeployPackageBuilder(IOptionsMonitor<PublishingSettings> settings, IDeploymentPackageContractReader contractReader)
    {
        this.settings = settings;
        this.contractReader = contractReader;
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> StageAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WriteStagedArtifacts(context);
        return Task.FromResult(PublishingStepResult.Success("Deployment package artifacts staged."));
    }

    /// <inheritdoc />
    public Task<string> CreateInterimArchiveAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = context.Manifest ?? throw new InvalidOperationException("Manifest is required.");
        var staging = context.StagingDirectory ?? throw new InvalidOperationException("Staging directory is required.");

        WriteChecksums(staging);
        _ = contractReader.ReadFromDirectory(staging);
        var interimPath = Path.Combine(context.WorkingDirectory!, $"{manifest.Service}-{manifest.Version}.interim{DeployPackageLayout.Extension}");
        CreateArchive(staging, interimPath);
        context.InterimPackagePath = interimPath;
        return Task.FromResult(interimPath);
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> FinalizeAsync(
        PublishingContext context,
        DeploymentReadinessReport report,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = context.Manifest ?? throw new InvalidOperationException("Manifest is required.");
        var staging = context.StagingDirectory ?? throw new InvalidOperationException("Staging directory is required.");

        PublishingStagingArtifacts.EnsureServiceImageTarInStaging(context);

        var packageChecksum = ComputeDirectoryChecksum(staging);
        var certificate = new DeploymentReadinessCertificate
        {
            ValidationId = Guid.NewGuid().ToString("N"),
            ValidatedUtc = DateTimeOffset.UtcNow,
            DrvVersion = Aovias.DeploymentValidation.Rules.EnvironmentNeutralRules.DrvVersion,
            RuntimeCompatibility = ResolveRuntimeCompatibility(context),
            PackageChecksum = packageChecksum,
            ValidationScore = report.OverallScore,
            ValidatorResults = report.StageResults
        };

        File.WriteAllText(
            Path.Combine(staging, DeployPackageLayout.DeploymentReadinessCertificateFile),
            JsonSerializer.Serialize(certificate, JsonOptions));

        var checksumCount = Directory.GetFiles(staging)
            .Count(file => !string.Equals(Path.GetFileName(file), DeployPackageLayout.ChecksumsFile, StringComparison.OrdinalIgnoreCase)) + 1;
        var publishingReport = Reporting.PublishingReportBuilder.Build(context, context.Request.Profile, report, checksumCount);
        File.WriteAllText(
            Path.Combine(staging, DeployPackageLayout.PublishingReportFile),
            JsonSerializer.Serialize(publishingReport, JsonOptions));

        WriteChecksums(staging);
        _ = contractReader.ReadFromDirectory(staging);

        var outputDirectory = context.Request.OutputDirectory ?? settings.CurrentValue.OutputFolder;
        Directory.CreateDirectory(outputDirectory);
        var packageFileName = PublishingImageNaming.GetPackageFileName(manifest.Service);
        var packagePath = Path.Combine(outputDirectory, packageFileName);
        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        CreateArchive(staging, packagePath);
        context.PackagePath = packagePath;
        context.GeneratedFiles.Add(packagePath);
        return Task.FromResult(PublishingStepResult.Success($"Created {packagePath}"));
    }

    private static void WriteStagedArtifacts(PublishingContext context)
    {
        var manifest = context.Manifest ?? throw new InvalidOperationException("Manifest is required.");
        var health = context.Health ?? throw new InvalidOperationException("Health definition is required.");
        var metadata = context.Metadata ?? throw new InvalidOperationException("Metadata is required.");
        var staging = context.StagingDirectory ?? throw new InvalidOperationException("Staging directory is required.");

        Directory.CreateDirectory(staging);

        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.ManifestFile), JsonSerializer.Serialize(manifest, JsonOptions));
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.HealthFile), JsonSerializer.Serialize(health, JsonOptions));
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.MetadataFile), JsonSerializer.Serialize(metadata, JsonOptions));
        File.WriteAllText(
            Path.Combine(staging, DeployPackageLayout.ConfigTemplateFile),
            ResolveConfigurationTemplateJson(context));
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.ComposeFile), context.ComposeContent ?? string.Empty);
        File.WriteAllText(
            Path.Combine(staging, DeployPackageLayout.EnvTemplateFile),
            context.EnvTemplateContent ?? string.Empty);

        if (string.IsNullOrWhiteSpace(context.DockerImageTarPath) || !File.Exists(context.DockerImageTarPath))
        {
            throw new InvalidOperationException("Docker image tar was not found.");
        }

        var destinationTar = Path.Combine(staging, DeployPackageLayout.ServiceImageTar);
        if (!string.Equals(
                Path.GetFullPath(context.DockerImageTarPath),
                Path.GetFullPath(destinationTar),
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(context.DockerImageTarPath, destinationTar, overwrite: true);
        }

        if (context.DeploymentContracts is not null)
        {
            Aovias.Deployment.Contracts.Reading.ServiceDeploymentBundleIO.WriteDirectory(staging, context.DeploymentContracts);
            _ = InfrastructureContractPublisher.Write(staging, context.DeploymentContracts, context.BuildIdentity);
        }
    }

    private string ResolveRuntimeCompatibility(PublishingContext context)
    {
        var os = settings.CurrentValue.TargetRuntimeOs;
        var framework = context.Discovery?.Framework;
        return string.IsNullOrWhiteSpace(framework)
            ? os
            : $"{os}/{framework}";
    }

    private static string ResolveConfigurationTemplateJson(PublishingContext context)
    {
        var json = context.ConfigurationTemplateJson;
        if (string.IsNullOrWhiteSpace(json))
        {
            json = JsonSerializer.Serialize(context.ConfigurationTemplate, JsonOptions);
        }

        return DeploymentArtifactSanitizer.PrepareConfigurationTemplateJson(json);
    }

    private static void WriteChecksums(string staging)
    {
        var checksums = ComputeChecksums(staging);
        File.WriteAllText(
            Path.Combine(staging, DeployPackageLayout.ChecksumsFile),
            JsonSerializer.Serialize(checksums, JsonOptions));
    }

    private static void CreateArchive(string staging, string packagePath)
    {
        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        ZipFile.CreateFromDirectory(staging, packagePath);
    }

    private static Dictionary<string, string> ComputeChecksums(string directory)
    {
        var checksums = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, DeployPackageLayout.ChecksumsFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            checksums[name] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
        }

        return checksums;
    }

    private static string ComputeDirectoryChecksum(string directory)
    {
        var checksums = ComputeChecksums(directory);
        var payload = string.Join("|", checksums.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}:{pair.Value}"));
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
