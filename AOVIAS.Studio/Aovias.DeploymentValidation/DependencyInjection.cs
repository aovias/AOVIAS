using Aovias.Deployment.Contracts.Reading;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Services;
using Aovias.DeploymentValidation.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.DeploymentValidation;

/// <summary>
/// Dependency injection extensions for deployment readiness validation.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAoviasDeploymentValidation(this IServiceCollection services)
    {
        services.AddSingleton<IDeploymentPackageContractReader, DeploymentPackageContractReader>();
        services.AddSingleton<DeploymentValidationLogger>();
        services.AddSingleton<ValidationWorkspaceManager>();
        services.AddSingleton<IValidationCommandRunner, ValidationCommandRunner>();
        services.AddSingleton<ITemporaryDeploymentWorkspaceFactory, TemporaryDeploymentWorkspaceFactory>();
        services.AddSingleton<IDeploymentValidationStage, PackageValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, ContractValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, DockerImageValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, ComposeValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, BootstrapManifestValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, RuntimeConfigurationValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, InfrastructureProvisioningValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, RuntimeBootstrapValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, ContainerStartupValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, HealthValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, RollbackValidationStage>();
        services.AddSingleton<IDeploymentValidationStage, EnvironmentNeutralValidationStage>();
        services.AddSingleton<Aovias.Deployment.Contracts.Smoke.IServiceSmokeTestRunner, Aovias.Deployment.Contracts.Smoke.ServiceSmokeTestRunner>();
        services.AddSingleton<IPlatformDeploymentSimulationService, PlatformDeploymentSimulationService>();
        services.AddSingleton<IDeploymentValidator, DeploymentValidationEngine>();
        return services;
    }
}
