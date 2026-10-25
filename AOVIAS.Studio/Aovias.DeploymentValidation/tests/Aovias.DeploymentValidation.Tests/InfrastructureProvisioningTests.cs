using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.Deployment.Contracts.Reading;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class InfrastructureProvisioningTests
{
    [Fact]
    public void InfrastructureContractBuilder_AggregatesServiceRequirements()
    {
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "compliance-service",
            DisplayName = "Compliance Service",
            Version = "1.0.0",
            ImageReference = "aovias/compliance-service:1.0.0",
            ContainerName = "compliance-service",
            EntryPoint = "ComplianceService.dll",
            HealthEndpoint = "/health",
            Ports = [5001],
            Dependencies = ["gateway", "organization-service"]
        });

        var contract = InfrastructureContractBuilder.Build(bundle);

        Assert.Equal("compliance-service", contract.ServiceId);
        Assert.NotEmpty(contract.Filesystem.Directories);
        Assert.True(contract.Database.Required);
        Assert.Contains(contract.Providers, item => item.Provider == "RabbitMQ" && item.Required);
        Assert.Equal("aovias/compliance-service:1.0.0", contract.Docker.ImageReference);
    }

    [Fact]
    public void WriteDirectory_DoesNotGenerateInfrastructureContract()
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
            Ports = [5212]
        });

        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            ServiceDeploymentBundleIO.WriteDirectory(directory, bundle);
            Assert.False(File.Exists(Path.Combine(directory, DeploymentPackageLayout.InfrastructureContractFile)));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Pipeline_ProvisionFilesystemSecretsAndCertificates()
    {
        const string password = "infra-test-password";
        var configRoot = CreateConfigRoot(password);
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "identity-service",
            DisplayName = "Identity Service",
            Version = "1.0.0",
            ImageReference = "aovias/identity-service:1.0.0",
            ContainerName = "identity-service",
            EntryPoint = "IdentityService.dll",
            HealthEndpoint = "/health",
            Ports = [5062]
        });

        var hostPaths = new HostPathContext
        {
            HostConfigRoot = configRoot,
            HostLogsRoot = Path.Combine(configRoot, "logs"),
            HostPackagesRoot = Path.Combine(configRoot, "packages")
        };

        CreateSigningPfx(configRoot, bundle, password);

        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = InfrastructureContractBuilder.Build(bundle),
            HostPaths = hostPaths,
            RuntimeCapabilities = bundle.RuntimeCapabilities
        };

        var report = await InfrastructureProvisioningPipeline.ExecuteAsync(context);

        Assert.True(report.DeploymentAllowed, string.Join("; ", report.Blockers));
        Assert.Contains(report.Stages, stage => stage.Stage == "Filesystem" && stage.Passed);
        Assert.Contains(report.Stages, stage => stage.Stage == "Secrets" && stage.Passed);
        Assert.Contains(report.Stages, stage => stage.Stage == "Certificates" && stage.Passed);

        Directory.Delete(configRoot, recursive: true);
    }

    [Fact]
    public async Task Pipeline_StopsBeforeLaterStagesWhenSecretsMissing()
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(configRoot, "secrets", "local"));
        File.WriteAllText(
            Path.Combine(configRoot, "secrets", "local", "settings.secrets.json"),
            """{"jwtSigningKey":"only-jwt"}""");

        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "identity-service",
            DisplayName = "Identity Service",
            Version = "1.0.0",
            ImageReference = "aovias/identity-service:1.0.0",
            ContainerName = "identity-service",
            EntryPoint = "IdentityService.dll",
            HealthEndpoint = "/health",
            Ports = [5062]
        });

        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = InfrastructureContractBuilder.Build(bundle),
            HostPaths = new HostPathContext
            {
                HostConfigRoot = configRoot,
                HostLogsRoot = Path.Combine(configRoot, "logs"),
                HostPackagesRoot = Path.Combine(configRoot, "packages")
            },
            RuntimeCapabilities = bundle.RuntimeCapabilities
        };

        var report = await InfrastructureProvisioningPipeline.ExecuteAsync(context);

        Assert.False(report.DeploymentAllowed);
        Assert.NotEmpty(report.Blockers);

        Directory.Delete(configRoot, recursive: true);
    }

    private static string CreateConfigRoot(string certificatePassword)
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var secretsDirectory = Path.Combine(configRoot, "secrets", "local");
        Directory.CreateDirectory(secretsDirectory);
        File.WriteAllText(
            Path.Combine(secretsDirectory, "settings.secrets.json"),
            JsonSerializer.Serialize(new
            {
                certificatePassword,
                jwtSigningKey = "test-jwt",
                databaseConnectionString = "Server=.;Database=Test;Trusted_Connection=True;"
            }));
        return configRoot;
    }

    private static void CreateSigningPfx(string configRoot, ServiceDeploymentBundle bundle, string password)
    {
        var requirement = bundle.Certificate.Certificates[0];
        var certificatePath = ContractCertificateProvisioner.ResolveCertificatePath(configRoot, requirement);
        Directory.CreateDirectory(Path.GetDirectoryName(certificatePath)!);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Test Signing",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, password));
    }
}
