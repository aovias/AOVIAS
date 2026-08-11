using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Publishing.Providers.Static;

/// <summary>
/// Static web site publishing provider.
/// </summary>
public sealed class StaticWebProjectTypeProvider : IProjectTypeProvider
{
    /// <inheritdoc />
    public string ProviderName => "Static";

    /// <inheritdoc />
    public bool CanDiscover(string projectPath)
    {
        var root = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath) ?? projectPath;
        return File.Exists(Path.Combine(root, "index.html")) &&
               !File.Exists(Path.Combine(root, "angular.json")) &&
               !Directory.EnumerateFiles(root, "*.csproj").Any();
    }

    /// <inheritdoc />
    public Task<ProjectDiscoveryResult?> DiscoverAsync(string projectPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath) ?? projectPath;
        if (!CanDiscover(root))
        {
            return Task.FromResult<ProjectDiscoveryResult?>(null);
        }

        var serviceName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Task.FromResult<ProjectDiscoveryResult?>(new ProjectDiscoveryResult
        {
            ProjectType = PublishingProjectType.StaticWeb,
            ProjectPath = Path.Combine(root, "index.html"),
            ProjectRoot = root,
            ServiceId = serviceName,
            ServiceName = serviceName,
            ProviderName = ProviderName,
            Confidence = 80,
            ContainerPort = 80,
            HealthEndpoint = "/",
            Framework = "Static"
        });
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> ValidateAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("Static site validated."));

    /// <inheritdoc />
    public Task<PublishingStepResult> RestoreDependenciesAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("No dependencies required."));

    /// <inheritdoc />
    public Task<PublishingStepResult> BuildAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("Static build not required."));

    /// <inheritdoc />
    public Task<PublishingStepResult> PublishAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        PublishingFileSystem.CopyDirectory(context.Discovery!.ProjectRoot, Path.Combine(context.DockerBuildContext!, "static"));
        return Task.FromResult(PublishingStepResult.Success("Static content staged."));
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> InspectConfigurationAsync(PublishingContext context, CancellationToken cancellationToken)
        => Task.FromResult(PublishingStepResult.Success("Static configuration inspected."));
}

public static class DependencyInjection
{
    public static IServiceCollection AddStaticPublishingProvider(this IServiceCollection services)
    {
        services.AddSingleton<IProjectTypeProvider, StaticWebProjectTypeProvider>();
        return services;
    }
}
