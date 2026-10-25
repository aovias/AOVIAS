using System.Xml.Linq;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Discovery;

/// <summary>
/// Scans the platform repository and discovers deployable applications without hardcoded project names.
/// </summary>
public sealed class SolutionDeployableProjectDiscoverer : IDeployableProjectDiscoverer
{
    private readonly IProjectTypeProviderRegistry providerRegistry;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<SolutionDeployableProjectDiscoverer> logger;

    public SolutionDeployableProjectDiscoverer(
        IProjectTypeProviderRegistry providerRegistry,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<SolutionDeployableProjectDiscoverer> logger)
    {
        this.providerRegistry = providerRegistry;
        this.settings = settings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeployableProject>> DiscoverAsync(string? platformRoot = null, CancellationToken cancellationToken = default)
    {
        var root = PlatformRootResolver.Resolve(platformRoot ?? settings.CurrentValue.PlatformProjectsRoot);
        var version = settings.CurrentValue.DefaultRuntime;
        var outputPath = settings.CurrentValue.OutputFolder;
        var candidates = new List<string>();

        foreach (var scanRoot in GetScanRoots(root))
        {
            if (!Directory.Exists(scanRoot))
            {
                continue;
            }

            foreach (var projectPath in Directory.EnumerateFiles(scanRoot, "*.csproj", SearchOption.AllDirectories))
            {
                if (IsIgnoredPath(projectPath))
                {
                    continue;
                }

                var workingDirectory = Path.GetDirectoryName(projectPath)!;
                if (!File.Exists(Path.Combine(workingDirectory, "Program.cs")))
                {
                    continue;
                }

                candidates.Add(projectPath);
            }
        }

        var webAppRoot = PlatformRootResolver.ResolveWebAppRoot(root);
        if (!string.IsNullOrWhiteSpace(webAppRoot) && File.Exists(Path.Combine(webAppRoot, "angular.json")))
        {
            candidates.Add(webAppRoot);
        }

        var runtimeAgentProject = PlatformRootResolver.ResolveRuntimeAgentProjectPath(root);
        if (File.Exists(runtimeAgentProject) && !candidates.Contains(runtimeAgentProject, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(runtimeAgentProject);
        }

        var discovered = new List<(ProjectDiscoveryResult Discovery, DeploymentMetadata Metadata)>();
        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var discoveryPath = Directory.Exists(candidate) ? candidate : candidate;
            var discovery = await providerRegistry.DiscoverAsync(discoveryPath, cancellationToken);
            if (discovery is null)
            {
                logger.LogDebug("Skipping {Path}: no publishing provider matched.", candidate);
                continue;
            }

            var metadata = DeploymentMetadataReader.TryLoad(discovery.ProjectPath, discovery.ProjectRoot);
            if (metadata is null)
            {
                logger.LogWarning("Skipping {Path}: missing {MetadataFile}.", candidate, DeploymentMetadataReader.FileName);
                continue;
            }

            if (discovered.Any(item => item.Metadata.ServiceId.Equals(metadata.ServiceId, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogDebug("Skipping duplicate service id {ServiceId} for {Path}.", metadata.ServiceId, candidate);
                continue;
            }

            discovered.Add((discovery, metadata));
        }

        var graph = PublishingDependencyGraphBuilder.BuildGraph(
            discovered.Select(item => (item.Metadata.ServiceId, item.Metadata.GetRequiredServiceDependencies())));
        var startupOrder = PublishingDependencyGraphBuilder.TopologicalSort(graph);

        var projects = discovered.Select(item => new DeployableProject
        {
            Name = item.Metadata.DisplayName ?? DeployableProjectNaming.ToDisplayName(item.Discovery, item.Metadata.ServiceId),
            ServiceId = item.Metadata.ServiceId,
            ProjectPath = item.Discovery.ProjectPath,
            DiscoveryPath = item.Discovery.ProjectRoot,
            Type = item.Discovery.ProjectType,
            Dockerfile = ResolveDockerfileHint(item.Discovery),
            OutputPath = outputPath,
            Version = version,
            StartupOrder = PublishingDependencyGraphBuilder.GetStartupOrder(item.Metadata.ServiceId, startupOrder),
            ContainerPort = item.Metadata.Ports.ContainerPort,
            HealthEndpoint = item.Metadata.Health.Endpoint,
            DependsOn = item.Metadata.GetRequiredServiceDependencies(),
            ReleaseLibraryCategory = item.Metadata.ReleaseLibraryCategory,
            IncludeInDemoBundle = item.Metadata.IncludeInDemoBundle,
            IncludeInSolutionDeployment = item.Metadata.IncludeInSolutionDeployment
        }).ToList();

        var validation = PublishingContractValidationService.ValidateSolutionGraph(projects);
        if (!validation.Passed)
        {
            throw new InvalidOperationException(
                $"Solution deployment metadata validation failed: {string.Join("; ", validation.Errors)}");
        }

        return projects
            .OrderBy(project => project.StartupOrder)
            .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> GetScanRoots(string platformRoot)
    {
        yield return Path.Combine(platformRoot, "AOVIAS.Command", "ComplaincePlatform");
        var legacyCommandRoot = Path.Combine(platformRoot, "ComplaincePlatform");
        if (Directory.Exists(legacyCommandRoot))
        {
            yield return legacyCommandRoot;
        }

        yield return Path.Combine(platformRoot, "AOVIAS.Studio", "Aovias.Runtime.Agent");
    }

    private static bool IsIgnoredPath(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
           || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
           || path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
           || path.Contains($"{Path.DirectorySeparatorChar}.artifacts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
           || path.Contains(".Tests", StringComparison.OrdinalIgnoreCase)
           || path.Contains("ComplianceServiceManager", StringComparison.OrdinalIgnoreCase)
           || path.Contains("D0RuntimeVerifier", StringComparison.OrdinalIgnoreCase);

    private static string? ResolveDockerfileHint(ProjectDiscoveryResult discovery)
    {
        var dockerfile = Path.Combine(discovery.ProjectRoot, "Dockerfile");
        return File.Exists(dockerfile) ? dockerfile : null;
    }
}
