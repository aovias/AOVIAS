using Aovias.DeploymentValidation;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Contracts.Verification;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Execution;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Orchestration;
using Aovias.Publishing.Core.Packaging;
using Aovias.Publishing.Core.Pipeline;
using Aovias.Publishing.Core.ReleaseLibrary;
using Aovias.Publishing.Core.Verification;
using Aovias.Publishing.Core.Workspace;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Publishing.Core;

/// <summary>
/// Dependency injection extensions for publishing core services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers publishing core services.
    /// </summary>
    public static IServiceCollection AddPublishingCore(this IServiceCollection services)
    {
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IDockerCliExecutor, DockerCliExecutor>();
        services.AddSingleton<IDotNetCliExecutor, DotNetCliExecutor>();
        services.AddSingleton<INodeCliExecutor, NodeCliExecutor>();
        services.AddSingleton<IDeployManifestGenerator, DeployManifestGenerator>();
        services.AddSingleton<IHealthDefinitionGenerator, HealthDefinitionGenerator>();
        services.AddSingleton<IConfigTemplateGenerator, ConfigTemplateGenerator>();
        services.AddSingleton<IEnvTemplateGenerator, EnvironmentFileGenerator>();
        services.AddSingleton<IComposeGenerator, ComposeGenerator>();
        services.AddSingleton<IDockerfileGenerator, DockerfileGenerator>();
        services.AddSingleton<IDeployPackageBuilder, DeployPackageBuilder>();
        services.AddSingleton<IDeployPackageReader, DeployPackageReader>();
        services.AddSingleton<IDeployPackageValidator, DeployPackageValidator>();
        services.AddSingleton<IDeployPackageVerifier, DeployPackageVerifier>();
        services.AddSingleton<IPackageVerifier, PackageVerifier>();
        services.AddSingleton<IGoldenPackageRepository, GoldenPackageRepository>();
        services.AddSingleton<IReleaseLibraryService, ReleaseLibraryService>();
        services.AddSingleton<PublishingBuildIdentityService>();
        services.AddSingleton<IDockerImageIdentityResolver, DockerImageIdentityResolver>();
        services.AddSingleton<IRuntimeAgentPackageVerifier, RuntimeAgentPackageVerifier>();
        services.AddSingleton<IPublishingWorkspaceManager, PublishingWorkspaceManager>();
        services.AddSingleton<IPublishingLogWriter, PublishingLogWriter>();
        services.AddSingleton<PublishingStageExecutor>();
        services.AddSingleton<IPublishingPipelineOrchestrator, PublishingPipelineOrchestrator>();
        services.AddSingleton<IProjectTypeProviderRegistry, ProjectTypeProviderRegistry>();
        services.AddSingleton<IDeployableProjectDiscoverer, SolutionDeployableProjectDiscoverer>();
        services.AddSingleton<ISolutionPublishingOrchestrator, SolutionPublishingOrchestrator>();
        services.AddSingleton<ISolutionPublishingPreflight, SolutionPublishingPreflightService>();
        services.AddSingleton<ISolutionPackageInspector, SolutionPackageInspector>();
        services.AddSingleton<IPublishingPipeline, PublishingPipeline>();
        services.AddSingleton<IPlatformVerificationPipeline, PlatformVerificationPipeline>();
        services.AddAoviasDeploymentValidation();
        return services;
    }
}
