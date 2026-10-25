using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.Packages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.PackageBuilder.Services;

/// <summary>
/// Validates AOVIAS package archives against platform rules.
/// </summary>
public sealed class PackageValidator : Abstractions.IPackageValidator
{
    private readonly Abstractions.IPackageReader packageReader;
    private readonly Abstractions.IChecksumProvider checksumProvider;
    private readonly Abstractions.IPackageSigner packageSigner;
    private readonly IDeployPackageValidator deployPackageValidator;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly ILogger<PackageValidator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageValidator"/> class.
    /// </summary>
    public PackageValidator(
        Abstractions.IPackageReader packageReader,
        Abstractions.IChecksumProvider checksumProvider,
        Abstractions.IPackageSigner packageSigner,
        IDeployPackageValidator deployPackageValidator,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        ILogger<PackageValidator> logger)
    {
        this.packageReader = packageReader;
        this.checksumProvider = checksumProvider;
        this.packageSigner = packageSigner;
        this.deployPackageValidator = deployPackageValidator;
        this.releaseSettings = releaseSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<PackageValidationResult> ValidateAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        PackageContents? contents = null;

        try
        {
            if (!File.Exists(packagePath))
            {
                errors.Add("Package file was not found.");
                return CreateResult(false, errors, warnings, contents);
            }

            var isDeployPackage = packagePath.EndsWith(DeployPackageLayout.Extension, StringComparison.OrdinalIgnoreCase);
            var isLegacyPackage = packagePath.EndsWith(PackageLayout.Extension, StringComparison.OrdinalIgnoreCase);
            if (!isDeployPackage && !isLegacyPackage)
            {
                errors.Add("Package must use the .aovpkg or .aovdeploy extension.");
            }

            var fileInfo = new FileInfo(packagePath);
            var maxSize = releaseSettings.CurrentValue.MaximumPackageSizeBytes;
            if (fileInfo.Length > maxSize)
            {
                errors.Add($"Package exceeds maximum size of {maxSize} bytes.");
            }

            if (isDeployPackage)
            {
                var deployValidation = await deployPackageValidator.ValidateAsync(packagePath, cancellationToken);
                errors.AddRange(deployValidation.Errors);
                warnings.AddRange(deployValidation.Warnings);
            }

            contents = await packageReader.ReadAsync(packagePath, cancellationToken);
            ValidateManifest(contents.Manifest, errors, warnings, isDeployPackage);

            if (isLegacyPackage)
            {
                await ValidateChecksumsAsync(packagePath, contents, errors, warnings, cancellationToken);

                if (await packageSigner.VerifyAsync(packagePath, cancellationToken) == false)
                {
                    warnings.Add("Package signature verification failed or signature is not yet configured.");
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Package validation failed for {PackagePath}", packagePath);
            errors.Add(exception.Message);
        }

        return CreateResult(errors.Count == 0, errors, warnings, contents);
    }

    private void ValidateManifest(
        PackageManifest manifest,
        ICollection<string> errors,
        ICollection<string> warnings,
        bool isDeployPackage)
    {
        var settings = releaseSettings.CurrentValue;

        if (string.IsNullOrWhiteSpace(manifest.PackageId))
        {
            errors.Add("Manifest PackageId is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.PackageVersion))
        {
            errors.Add("Manifest PackageVersion is required.");
        }

        if (!Version.TryParse(manifest.MinimumRuntimeVersion, out _))
        {
            warnings.Add("MinimumRuntimeVersion is not a valid semantic version.");
        }
        else if (!isDeployPackage &&
                 Version.TryParse(settings.SupportedRuntimeVersion, out var supportedRuntime) &&
                 Version.TryParse(manifest.MinimumRuntimeVersion, out var minimumRuntime) &&
                 minimumRuntime > supportedRuntime)
        {
            errors.Add(
                $"Package requires runtime {manifest.MinimumRuntimeVersion} but platform supports {settings.SupportedRuntimeVersion}.");
        }
        else if (isDeployPackage && !Version.TryParse(settings.SupportedRuntimeVersion, out _))
        {
            warnings.Add("Configured SupportedRuntimeVersion is not a valid semantic version.");
        }
    }

    private async Task ValidateChecksumsAsync(
        string packagePath,
        PackageContents contents,
        ICollection<string> errors,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        if (contents.Checksums.Count == 0)
        {
            errors.Add("Checksum file is missing or empty.");
            return;
        }

        var actualPackageHash = await checksumProvider.ComputeFileHashAsync(packagePath, cancellationToken);
        if (!string.Equals(contents.Manifest.PackageHash, actualPackageHash, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Manifest PackageHash does not match the current archive hash. The package may have been modified after build.");
        }
    }

    private static PackageValidationResult CreateResult(
        bool isValid,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        PackageContents? contents)
        => new()
        {
            IsValid = isValid,
            Errors = errors,
            Warnings = warnings,
            Contents = contents
        };
}
