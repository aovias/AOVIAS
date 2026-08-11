using System.Text.RegularExpressions;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// Prepares Docker build context for the Runtime Agent monorepo Dockerfile.
/// </summary>
internal static class RuntimeAgentDockerContextStager
{
    private static readonly string[] FallbackMonorepoFolders =
    [
        "Aovias.Configuration",
        "Aovias.Deployment.Contracts",
        "Aovias.DeploymentValidation",
        "Aovias.Orchestration",
        "Aovias.Release",
        "Aovias.Publishing",
        "Aovias.Runtime.Agent"
    ];

    private static readonly string[] ExcludedDirectoryNames = [".git", ".vs", "bin", "obj", "node_modules", "tests"];

    private static readonly Regex CopyRootPattern = new(
        @"^COPY\s+(Aovias\.[^/\s]+)/",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    internal static bool UsesMonorepoDockerfile(string dockerfileContent)
        => dockerfileContent.Contains("COPY Aovias.Configuration/", StringComparison.Ordinal)
           || dockerfileContent.Contains("COPY Aovias.Runtime.Agent/src/", StringComparison.Ordinal);

    internal static bool IsRuntimeAgentDiscovery(string serviceId, string projectRoot)
        => RuntimeAgentConventions.IsRuntimeAgent(serviceId, projectRoot);

    internal static string? TryResolvePlatformRoot(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Aovias.Configuration"))
                && Directory.Exists(Path.Combine(current.FullName, "Aovias.Runtime.Agent", "src")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    internal static IReadOnlyList<string> ResolveRequiredMonorepoFolders(string dockerfileContent)
    {
        var folders = CopyRootPattern.Matches(dockerfileContent)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return folders.Count == 0 ? FallbackMonorepoFolders : folders;
    }

    internal static string Stage(string platformRoot, string stagingRoot, string dockerfileContent)
    {
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, recursive: true);
        }

        Directory.CreateDirectory(stagingRoot);

        foreach (var folder in ResolveRequiredMonorepoFolders(dockerfileContent))
        {
            var source = Path.Combine(platformRoot, folder);
            if (!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException(
                    $"Required monorepo folder '{folder}' was not found at '{source}'. "
                    + "Ensure the platform root contains all projects referenced by the Runtime Agent Dockerfile.");
            }

            CopyDirectoryExcludingBuildArtifacts(source, Path.Combine(stagingRoot, folder));
        }

        ValidateStagedContext(stagingRoot, dockerfileContent);
        ValidateDeploymentPathPolicy(stagingRoot);
        return Path.GetFullPath(stagingRoot);
    }

    internal static void ValidateDeploymentPathPolicy(string buildContext)
    {
        var sessionStorePath = Path.Combine(
            buildContext,
            "Aovias.Runtime.Agent",
            "src",
            "Aovias.Runtime.Agent.Infrastructure",
            "Deployment",
            "DeploymentSessionStore.cs");

        if (!File.Exists(sessionStorePath))
        {
            throw new FileNotFoundException(
                "Staged Runtime Agent deployment sources are missing DeploymentSessionStore.cs.",
                sessionStorePath);
        }

        var content = File.ReadAllText(sessionStorePath);
        if (content.Contains("Environment.SpecialFolder.CommonApplicationData", StringComparison.Ordinal)
            || content.Contains("new DeploymentSessionStore()", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Staged Runtime Agent still uses forbidden deployment session paths under CommonApplicationData. "
                + "Republish from the current platform source tree.");
        }

        if (!content.Contains("RuntimeAgentDeploymentPathResolver", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Staged Runtime Agent deployment session store must resolve paths via RuntimeAgentDeploymentPathResolver.");
        }

        ValidateSignalRDiscoverySources(buildContext);
    }

    internal static void ValidateSignalRDiscoverySources(string buildContext)
    {
        var controllerPath = Path.Combine(
            buildContext,
            "Aovias.Runtime.Agent",
            "src",
            "Aovias.Runtime.Agent.Api",
            "Controllers",
            "DeploymentAvailabilityController.cs");

        if (!File.Exists(controllerPath))
        {
            throw new FileNotFoundException("Staged deployment availability controller was not found.", controllerPath);
        }

        var content = File.ReadAllText(controllerPath);
        if (!content.Contains("RuntimeSignalRDiscoveryDto", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Staged Runtime Agent must expose SignalR discovery metadata from GET /api/deployment/status.");
        }
    }

    internal static void ValidateStagedContext(string buildContext, string dockerfileContent)
    {
        var missing = ResolveRequiredMonorepoFolders(dockerfileContent)
            .Where(folder => !Directory.Exists(Path.Combine(buildContext, folder)))
            .ToList();

        if (missing.Count > 0)
        {
            throw new DirectoryNotFoundException(
                $"Docker build context '{buildContext}' is missing required folders: {string.Join(", ", missing)}.");
        }
    }

    private static void CopyDirectoryExcludingBuildArtifacts(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            if (ShouldExclude(directory))
            {
                continue;
            }

            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            if (ShouldExclude(Path.GetDirectoryName(file) ?? source))
            {
                continue;
            }

            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static bool ShouldExclude(string path)
    {
        foreach (var segment in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (ExcludedDirectoryNames.Contains(segment, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
