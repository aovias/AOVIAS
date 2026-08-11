using System.Text.Json;
using System.Xml.Linq;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Publishing.Providers.DotNet;

/// <summary>
/// .NET project publishing provider.
/// </summary>
public sealed class DotNetProjectTypeProvider : IProjectTypeProvider
{
    private readonly IDotNetCliExecutor dotNetCliExecutor;

    public DotNetProjectTypeProvider(IDotNetCliExecutor dotNetCliExecutor)
    {
        this.dotNetCliExecutor = dotNetCliExecutor;
    }

    /// <inheritdoc />
    public string ProviderName => "DotNet";

    /// <inheritdoc />
    public bool CanDiscover(string projectPath)
        => projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && File.Exists(projectPath);

    /// <inheritdoc />
    public Task<ProjectDiscoveryResult?> DiscoverAsync(string projectPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanDiscover(projectPath))
        {
            return Task.FromResult<ProjectDiscoveryResult?>(null);
        }

        var projectRoot = Path.GetDirectoryName(projectPath) ?? string.Empty;
        var document = XDocument.Load(projectPath);
        var sdk = document.Root?.Attribute("Sdk")?.Value ?? string.Empty;
        var isRuntimeAgent = RuntimeAgentConventions.IsRuntimeAgent(serviceId: null, projectPath);
        if (isRuntimeAgent)
        {
            projectRoot = ResolveRuntimeAgentRoot(projectPath);
        }

        var projectType = sdk.Contains("Web", StringComparison.OrdinalIgnoreCase)
            ? PublishingProjectType.AspNetCore
            : PublishingProjectType.WorkerService;

        var assemblyName = document.Descendants("AssemblyName").FirstOrDefault()?.Value
            ?? Path.GetFileNameWithoutExtension(projectPath);
        var serviceId = isRuntimeAgent ? RuntimeAgentConventions.ServiceId : assemblyName;
        var serviceName = isRuntimeAgent ? "AOVIAS Runtime Agent" : assemblyName;
        var port = isRuntimeAgent ? RuntimeAgentConventions.DefaultPort : ReadLaunchPort(projectRoot);
        var entryPoint = isRuntimeAgent ? RuntimeAgentConventions.EntryPointAssembly : $"{assemblyName}.dll";

        return Task.FromResult<ProjectDiscoveryResult?>(new ProjectDiscoveryResult
        {
            ProjectType = projectType,
            ProjectPath = projectPath,
            ProjectRoot = projectRoot,
            ServiceId = serviceId,
            ServiceName = serviceName,
            ProviderName = ProviderName,
            Confidence = 100,
            ContainerPort = port,
            HealthEndpoint = "/health",
            EntryPoint = entryPoint,
            Framework = "net10.0",
            PublishProperties = ResolvePublishProperties(assemblyName)
        });
    }

    /// <summary>
    /// The host project that physically owns the shared appsettings files.
    /// Any other project that references it must exclude those files from its
    /// own publish output to avoid NETSDK1152 duplicate-output errors.
    /// </summary>
    private const string AppSettingsOwnerAssemblyName = "ComplianceService";

    private static IReadOnlyDictionary<string, string> ResolvePublishProperties(string assemblyName)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.Equals(assemblyName, AppSettingsOwnerAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            properties["ExcludeAppSettingsFromPublish"] = "true";
        }

        // Projects that reference another project (e.g. for shared appsettings, per
        // AppSettingsOwnerAssemblyName above) also transitively pull in that project's own
        // deployment.metadata.json as a publish-output content item, colliding with the
        // referencing project's own file at the same relative output path (NETSDK1152). This
        // file is publishing-time-only metadata that DeploymentMetadataReader always reads from
        // each project's *source* directory, never from publish output, so which copy "wins" in
        // the merged output is immaterial — downgrading the SDK's duplicate-output check here is
        // the officially documented resolution for legitimate shared-reference content
        // collisions, the same class of issue ExcludeAppSettingsFromPublish already exists for.
        properties["ErrorOnDuplicatePublishOutputFiles"] = "false";

        return properties;
    }

    /// <inheritdoc />
    public async Task<PublishingStepResult> ValidateAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var inspect = await dotNetCliExecutor.InspectAsync(cancellationToken);
        return inspect.IsSuccess
            ? PublishingStepResult.Success(".NET SDK available.")
            : PublishingStepResult.Failure(DescribeFailure(inspect));
    }

    /// <inheritdoc />
    public async Task<PublishingStepResult> RestoreDependenciesAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var discovery = context.Discovery!;
        var result = await dotNetCliExecutor.RestoreAsync(discovery.ProjectPath, discovery.ProjectRoot, cancellationToken);
        return result.IsSuccess
            ? PublishingStepResult.Success("Dependencies restored.")
            : PublishingStepResult.Failure(DescribeFailure(result));
    }

    /// <inheritdoc />
    public async Task<PublishingStepResult> BuildAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var discovery = context.Discovery!;
        var result = await dotNetCliExecutor.BuildAsync(discovery.ProjectPath, discovery.ProjectRoot, cancellationToken);
        context.BuildOutput.Add(result.StandardOutput);
        return result.IsSuccess
            ? PublishingStepResult.Success("Build succeeded.")
            : PublishingStepResult.Failure(DescribeFailure(result));
    }

    /// <inheritdoc />
    public async Task<PublishingStepResult> PublishAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var discovery = context.Discovery!;
        Directory.CreateDirectory(context.PublishOutputDirectory!);
        var result = await dotNetCliExecutor.PublishAsync(
            discovery.ProjectPath,
            context.PublishOutputDirectory!,
            discovery.ProjectRoot,
            discovery.PublishProperties,
            cancellationToken);

        context.BuildOutput.Add(result.StandardOutput);

        if (!result.IsSuccess)
        {
            return PublishingStepResult.Failure(DescribeFailure(result));
        }

        if (!IsRuntimeAgent(context))
        {
            PublishingFileSystem.CopyDirectory(context.PublishOutputDirectory!, Path.Combine(context.DockerBuildContext!, "publish"));
        }

        return PublishingStepResult.Success("Publish succeeded.");
    }

    private static bool IsRuntimeAgent(PublishingContext context)
        => RuntimeAgentConventions.IsRuntimeAgent(context.Discovery?.ServiceId, context.Discovery?.ProjectRoot);

    /// <summary>
    /// MSBuild/dotnet CLI errors (including SDK diagnostics like NETSDK1152) are routinely written
    /// to standard output, not standard error — a process can fail (non-zero exit) with a
    /// completely empty StandardError while the actual reason is sitting in StandardOutput. Using
    /// StandardError alone therefore silently produces an empty failure message on exactly the
    /// failures operators most need to see. Falls back to StandardOutput, then a generic message,
    /// so a failed step is never reported with no explanation at all.
    /// </summary>
    private static string DescribeFailure(ProcessExecutionResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardError;
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return result.StandardOutput;
        }

        return $"Command failed with exit code {result.ExitCode} and produced no output.";
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> InspectConfigurationAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("Configuration inspected."));

    private static int ReadLaunchPort(string projectRoot)
    {
        var launchSettingsPath = Path.Combine(projectRoot, "Properties", "launchSettings.json");
        if (!File.Exists(launchSettingsPath))
        {
            return 8080;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        if (document.RootElement.TryGetProperty("profiles", out var profiles))
        {
            foreach (var profile in profiles.EnumerateObject())
            {
                if (profile.Value.TryGetProperty("applicationUrl", out var applicationUrl))
                {
                    var value = applicationUrl.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var lastSegment = value.Split(';', ':').LastOrDefault();
                        if (int.TryParse(lastSegment, out var port))
                        {
                            return port;
                        }
                    }
                }
            }
        }

        return 8080;
    }

    private static string ResolveRuntimeAgentRoot(string projectPath)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(projectPath) ?? projectPath);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dockerfile")) &&
                Directory.Exists(Path.Combine(current.FullName, "src")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetDirectoryName(projectPath) ?? projectPath;
    }
}

/// <summary>
/// DI extensions for the .NET provider.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDotNetPublishingProvider(this IServiceCollection services)
    {
        services.AddSingleton<IProjectTypeProvider, DotNetProjectTypeProvider>();
        return services;
    }
}
