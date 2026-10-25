using Aovias.Configuration.Settings;
using Aovias.PackageBuilder.Abstractions;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Identity;
using Aovias.Release.Contracts.Releases;
using Microsoft.Extensions.Options;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Generates deployment plans from validated release records.
/// </summary>
public sealed class ReleasePlanner : Abstractions.IReleasePlanner
{
    private static readonly string[] OrchestrationStepNames =
    [
        "Validate Package",
        "Verify Runtime",
        "Verify Dependencies",
        "Backup Current Release",
        "Docker Compose Stop",
        "Release Extraction",
        "Stage Release",
        "Load Docker Image",
        "Ensure Runtime Folders",
        "Docker Compose Up",
        "Configure Nginx",
        "Health Verification",
        "Deployment Acceptance",
        "Activate Release",
        "Register Deployment",
        "Register Runtime",
        "Audit"
    ];

    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IPackageReader packageReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasePlanner"/> class.
    /// </summary>
    public ReleasePlanner(
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IPackageReader packageReader)
    {
        this.releaseSettings = releaseSettings;
        this.packageReader = packageReader;
    }

    /// <inheritdoc />
    public async Task<DeploymentPlan> CreatePlanAsync(ReleaseRecord release, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = releaseSettings.CurrentValue;
        var manifest = release.Manifest;
        var compatibilityMessages = new List<string>();
        var dependencyMessages = new List<string>();
        var compatibilityPassed = true;
        var dependenciesValidated = true;

        if (Version.TryParse(manifest.MinimumRuntimeVersion, out var minimumRuntime) &&
            Version.TryParse(settings.SupportedRuntimeVersion, out var supportedRuntime) &&
            minimumRuntime > supportedRuntime)
        {
            compatibilityPassed = false;
            compatibilityMessages.Add(
                $"Package requires runtime {manifest.MinimumRuntimeVersion} but platform supports {settings.SupportedRuntimeVersion}.");
        }
        else
        {
            compatibilityMessages.Add("Runtime compatibility check passed.");
        }

        foreach (var dependency in manifest.Dependencies.Where(dependency => dependency.Required))
        {
            dependencyMessages.Add($"Required dependency {dependency.DisplayName} ({dependency.PackageId} {dependency.PackageVersion}) will be validated at deployment time.");
        }

        if (manifest.Dependencies.Count == 0)
        {
            dependencyMessages.Add("No package dependencies declared.");
        }

        var riskLevel = DetermineRisk(release);
        var actions = OrchestrationStepNames
            .Select((name, index) => new DeploymentAction
            {
                Name = name,
                Description = $"Orchestration step {index + 1}: {name}.",
                Order = index + 1,
                RequiresConfirmation = name is "Deployment Acceptance" or "Activate Release"
            })
            .ToList();

        DeploymentInspectionDetails? inspectionDetails = null;
        try
        {
            var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
            var packageInfo = new FileInfo(release.PackagePath);
            inspectionDetails = new DeploymentInspectionDetails
            {
                ServiceId = contents.Service.ServiceId,
                PackageVersion = contents.Manifest.PackageVersion,
                PackageSizeBytes = packageInfo.Exists ? packageInfo.Length : 0,
                DockerImageName = contents.Manifest.ServiceId,
                DockerImageTag = contents.Manifest.PackageVersion,
                ComposeServices = [contents.Service.ServiceId],
                HealthEndpoint = contents.Health.HealthEndpoint,
                ConfigTemplateKeys = contents.ArtifactPaths
                    .Where(path => path.Contains("config", StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Cast<string>()
                    .ToList(),
                Ports = [],
                Volumes = [],
                Dependencies = manifest.Dependencies.Select(dependency => $"{dependency.PackageId} {dependency.PackageVersion}").ToList(),
                EstimatedDuration = contents.Deployment.EstimatedDeploymentTime == default
                    ? TimeSpan.FromMinutes(8)
                    : contents.Deployment.EstimatedDeploymentTime,
                RiskLevel = riskLevel,
                RollbackAvailable = contents.Deployment.RollbackSupported
            };
        }
        catch
        {
            inspectionDetails = new DeploymentInspectionDetails
            {
                ServiceId = manifest.ServiceId,
                PackageVersion = manifest.PackageVersion,
                HealthEndpoint = "/health",
                EstimatedDuration = TimeSpan.FromMinutes(8),
                RiskLevel = riskLevel,
                RollbackAvailable = true
            };
        }

        var plan = new DeploymentPlan
        {
            PlanId = $"PLAN-{release.Identity.ReleaseId}",
            Release = release.Identity,
            Package = new PackageIdentity
            {
                PackageId = manifest.PackageId,
                PackageVersion = manifest.PackageVersion,
                ApplicationName = manifest.ApplicationName,
                ServiceId = manifest.ServiceId
            },
            CompatibilityPassed = compatibilityPassed,
            CompatibilityMessages = compatibilityMessages,
            DependenciesValidated = dependenciesValidated,
            DependencyMessages = dependencyMessages,
            RequiredActions = actions,
            EstimatedDuration = inspectionDetails.EstimatedDuration,
            RiskLevel = riskLevel,
            RollbackAvailable = true,
            AffectedServices = [manifest.ServiceId],
            OperatorConfirmationRequired = riskLevel >= DeploymentRiskLevel.Medium,
            CreatedAt = DateTimeOffset.UtcNow,
            InspectionDetails = inspectionDetails
        };

        return plan;
    }

    private static DeploymentRiskLevel DetermineRisk(ReleaseRecord release)
    {
        if (!release.Manifest.Dependencies.Any())
        {
            return DeploymentRiskLevel.Low;
        }

        return release.Manifest.Dependencies.Count > 2
            ? DeploymentRiskLevel.High
            : DeploymentRiskLevel.Medium;
    }
}
