using System.Text.Json;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Orchestration;

/// <summary>
/// Verifies release-library output after solution publishing.
/// </summary>
public sealed class SolutionPackageInspector : ISolutionPackageInspector
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] RequiredPackageFiles =
    [
        DeployPackageLayout.ServiceImageTar,
        DeployPackageLayout.ComposeFile,
        DeployPackageLayout.EnvTemplateFile,
        DeployPackageLayout.ManifestFile,
        DeployPackageLayout.MetadataFile,
        DeployPackageLayout.ChecksumsFile,
        DeployPackageLayout.HealthFile,
        DeployPackageLayout.DeploymentReadinessCertificateFile
    ];

    private readonly IDeployPackageVerifier packageVerifier;

    public SolutionPackageInspector(IDeployPackageVerifier packageVerifier)
    {
        this.packageVerifier = packageVerifier;
    }

    /// <inheritdoc />
    public async Task<SolutionPackageInspectionResult> InspectReleaseLibraryAsync(
        string releaseLibraryRoot,
        string? profile = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseLibraryRoot);

        var errors = new List<string>();
        var entries = new List<SolutionPackageInspectionEntry>();
        var isDemoProfile = string.Equals(profile, "demo", StringComparison.OrdinalIgnoreCase);

        if (!Directory.Exists(releaseLibraryRoot))
        {
            return new SolutionPackageInspectionResult
            {
                IsValid = false,
                Summary = $"Release library root was not found: {releaseLibraryRoot}",
                Errors = [$"Release library root was not found: {releaseLibraryRoot}"]
            };
        }

        var manifestPath = Path.Combine(releaseLibraryRoot, "solution-deployment.json");
        var bundlePath = Path.Combine(releaseLibraryRoot, "DemoDeployment.zip");

        if (!File.Exists(manifestPath))
        {
            errors.Add($"Missing solution manifest: {manifestPath}");
        }

        if (isDemoProfile && !File.Exists(bundlePath))
        {
            errors.Add($"Missing demo bundle: {bundlePath}");
        }

        if (File.Exists(manifestPath))
        {
            var manifest = JsonSerializer.Deserialize<SolutionDeploymentManifest>(
                File.ReadAllText(manifestPath),
                JsonOptions);

            if (manifest?.Services is { Count: > 0 })
            {
                foreach (var service in manifest.Services)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!service.DeploymentReady)
                    {
                        errors.Add($"{service.ServiceId}: package is not deployment ready.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(service.PackagePath) || !File.Exists(service.PackagePath))
                    {
                        errors.Add($"{service.ServiceId}: published package was not found at '{service.PackagePath}'.");
                        continue;
                    }

                    var category = !string.IsNullOrWhiteSpace(service.PackagePath)
                        ? Path.GetFileName(Path.GetDirectoryName(service.PackagePath)!) ?? "Services"
                        : "Services";
                    var entry = await InspectPackageAsync(service.ServiceId, category, service.PackagePath, cancellationToken);
                    entries.Add(entry);
                    if (!entry.IsValid)
                    {
                        errors.AddRange(entry.Errors);
                    }
                }

                if (isDemoProfile && entries.Count < manifest.Services.Count)
                {
                    var found = entries.Select(entry => entry.ServiceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var missing in manifest.Services
                                 .Select(service => service.ServiceId)
                                 .Where(id => !found.Contains(id)))
                    {
                        errors.Add($"Missing demo package for service '{missing}'.");
                    }
                }
            }
            else
            {
                errors.Add("Solution deployment manifest did not contain any services.");
            }
        }
        else
        {
            foreach (var categoryDirectory in Directory.EnumerateDirectories(releaseLibraryRoot))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var category = Path.GetFileName(categoryDirectory);
                foreach (var packagePath in Directory.EnumerateFiles(categoryDirectory, $"*{DeployPackageLayout.Extension}"))
                {
                    var serviceId = Path.GetFileNameWithoutExtension(packagePath);
                    var entry = await InspectPackageAsync(serviceId, category, packagePath, cancellationToken);
                    entries.Add(entry);
                    if (!entry.IsValid)
                    {
                        errors.AddRange(entry.Errors);
                    }
                }
            }
        }

        var isValid = errors.Count == 0 && entries.All(entry => entry.IsValid);
        return new SolutionPackageInspectionResult
        {
            IsValid = isValid,
            Summary = isValid
                ? $"Release library valid. {entries.Count} package(s) verified."
                : $"Release library inspection failed with {errors.Count} issue(s).",
            Packages = entries.OrderBy(entry => entry.ServiceId, StringComparer.OrdinalIgnoreCase).ToList(),
            SolutionDeploymentManifestPath = File.Exists(manifestPath) ? manifestPath : null,
            DemoDeploymentBundlePath = File.Exists(bundlePath) ? bundlePath : null,
            Errors = errors
        };
    }

    private async Task<SolutionPackageInspectionEntry> InspectPackageAsync(
        string serviceId,
        string category,
        string packagePath,
        CancellationToken cancellationToken)
    {
        var present = new List<string>();
        var missing = new List<string>();
        var errors = new List<string>();

        foreach (var required in RequiredPackageFiles)
        {
            if (await EntryExistsAsync(packagePath, required, cancellationToken))
            {
                present.Add(required);
            }
            else
            {
                missing.Add(required);
                errors.Add($"{serviceId}: missing {required}");
            }
        }

        var verification = await packageVerifier.VerifyAsync(packagePath, expectCertificate: true, cancellationToken);
        if (!verification.IsValid)
        {
            errors.AddRange(verification.Errors.Select(error => $"{serviceId}: {error}"));
        }

        return new SolutionPackageInspectionEntry
        {
            ServiceId = serviceId,
            Category = category,
            PackagePath = packagePath,
            IsValid = missing.Count == 0 && verification.IsValid,
            PresentFiles = present,
            MissingFiles = missing,
            Errors = errors
        };
    }

    private static async Task<bool> EntryExistsAsync(string packagePath, string entryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = File.OpenRead(packagePath);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        return archive.GetEntry(entryName) is not null;
    }
}
