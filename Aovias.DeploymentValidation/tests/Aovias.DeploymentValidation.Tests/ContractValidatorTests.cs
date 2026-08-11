using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Validation;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class ContractValidatorTests
{
    [Fact]
    public void Validate_GatewayBundle_Passes()
    {
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "gateway",
            DisplayName = "Gateway",
            Version = "1.0.0",
            ImageReference = "aovias/gateway:1.0.0",
            ContainerName = "gateway",
            EntryPoint = "Gateway.dll",
            HealthEndpoint = "/health",
            Ports = [5212],
            ConfigurationKeys = ["Jwt:Authority"]
        });

        var report = ContractValidator.Validate(bundle);

        Assert.True(report.Passed);
        Assert.All(report.Results, result => Assert.True(result.Passed, result.Name));
    }

    [Fact]
    public void Validate_IdentityBundle_IncludesSigningCertificateContract()
    {
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "identity-service",
            DisplayName = "Identity Service",
            Version = "1.0.0",
            ImageReference = "aovias/identity-service:1.0.0",
            ContainerName = "identity-service",
            EntryPoint = "IdentityService.dll",
            HealthEndpoint = "/health",
            Ports = [5062],
            ConfigurationKeys = ["ConnectionStrings:DefaultConnection"]
        });

        Assert.Contains(bundle.Certificate.Certificates, item => item.Id == "identity-signing" && item.Required);
        Assert.True(ContractValidator.Validate(bundle).Passed);
    }

    [Fact]
    public void Validate_RuntimeAgentBundle_IncludesAdditionalHealthProbes()
    {
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "runtime-agent",
            DisplayName = "Runtime Agent",
            Version = "1.0.0",
            ImageReference = "aovias/runtime-agent:1.0.0",
            ContainerName = "runtime-agent",
            EntryPoint = "Aovias.Runtime.Agent.Api.dll",
            HealthEndpoint = "/health",
            Ports = [7080]
        });

        Assert.NotEmpty(bundle.Health.AdditionalProbes);
        Assert.True(bundle.Feature.RequiresBootstrapDiagnostics);
        Assert.Contains(
            bundle.Volume.Mounts,
            mount => mount.ContainerPath == PlatformDeploymentDefaults.DeploymentRoot);
        Assert.Contains(
            bundle.Filesystem.Directories,
            directory => directory.Path == PlatformDeploymentDefaults.ReleasesRoot);
        Assert.True(ContractValidator.Validate(bundle).Passed);
    }
}
