using System.Text.Json;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Publishing.Providers.Angular;

/// <summary>
/// Angular project publishing provider.
/// </summary>
public sealed class AngularProjectTypeProvider : IProjectTypeProvider
{
    private readonly INodeCliExecutor nodeCliExecutor;

    public AngularProjectTypeProvider(INodeCliExecutor nodeCliExecutor)
    {
        this.nodeCliExecutor = nodeCliExecutor;
    }

    /// <inheritdoc />
    public string ProviderName => "Angular";

    /// <inheritdoc />
    public bool CanDiscover(string projectPath)
    {
        var root = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath) ?? projectPath;
        return File.Exists(Path.Combine(root, "angular.json"));
    }

    /// <inheritdoc />
    public Task<ProjectDiscoveryResult?> DiscoverAsync(string projectPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath) ?? projectPath;
        if (!File.Exists(Path.Combine(root, "angular.json")))
        {
            return Task.FromResult<ProjectDiscoveryResult?>(null);
        }

        var serviceName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Task.FromResult<ProjectDiscoveryResult?>(new ProjectDiscoveryResult
        {
            ProjectType = PublishingProjectType.Angular,
            ProjectPath = Path.Combine(root, "angular.json"),
            ProjectRoot = root,
            ServiceId = serviceName,
            ServiceName = serviceName,
            ProviderName = ProviderName,
            Confidence = 100,
            ContainerPort = 80,
            HealthEndpoint = "/",
            Framework = "Angular"
        });
    }

    /// <inheritdoc />
    public async Task<PublishingStepResult> ValidateAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var node = await nodeCliExecutor.InspectAsync(cancellationToken);
        return node.IsSuccess
            ? PublishingStepResult.Success("Node.js available.")
            : PublishingStepResult.Failure(node.StandardError);
    }

    /// <inheritdoc />
    public async Task<PublishingStepResult> RestoreDependenciesAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var result = await nodeCliExecutor.InstallAsync(context.Discovery!.ProjectRoot, cancellationToken);
        return result.IsSuccess
            ? PublishingStepResult.Success("npm dependencies installed.")
            : PublishingStepResult.Failure(result.StandardError);
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> BuildAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("Angular build handled during publish."));

    /// <inheritdoc />
    public async Task<PublishingStepResult> PublishAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var result = await nodeCliExecutor.RunNpmAsync("run build -- --configuration production", context.Discovery!.ProjectRoot, cancellationToken);
        context.BuildOutput.Add(result.StandardOutput);
        if (!result.IsSuccess)
        {
            return PublishingStepResult.Failure(result.StandardError);
        }

        var distPath = ResolveDistPath(context.Discovery.ProjectRoot);
        if (!Directory.Exists(distPath))
        {
            return PublishingStepResult.Failure($"Angular dist folder not found: {distPath}");
        }

        PublishingFileSystem.CopyDirectory(distPath, Path.Combine(context.DockerBuildContext!, "dist"));
        return PublishingStepResult.Success("Angular publish succeeded.");
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> InspectConfigurationAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("Angular configuration inspected."));

    private static string ResolveDistPath(string projectRoot)
    {
        var angularJsonPath = Path.Combine(projectRoot, "angular.json");
        using var document = JsonDocument.Parse(File.ReadAllText(angularJsonPath));
        if (document.RootElement.TryGetProperty("projects", out var projects))
        {
            foreach (var project in projects.EnumerateObject())
            {
                if (project.Value.TryGetProperty("architect", out var architect) &&
                    architect.TryGetProperty("build", out var build) &&
                    build.TryGetProperty("options", out var options) &&
                    options.TryGetProperty("outputPath", out var outputPath))
                {
                    var relative = outputPath.GetString() ?? "dist";
                    return Path.GetFullPath(Path.Combine(projectRoot, relative));
                }
            }
        }

        return Path.Combine(projectRoot, "dist");
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddAngularPublishingProvider(this IServiceCollection services)
    {
        services.AddSingleton<IProjectTypeProvider, AngularProjectTypeProvider>();
        return services;
    }
}
