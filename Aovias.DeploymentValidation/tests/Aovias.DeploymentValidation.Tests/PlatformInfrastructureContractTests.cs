using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Provisioning;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class PlatformInfrastructureContractTests
{
    public static IEnumerable<object[]> PlatformServices =>
    [
        ["runtime-agent", 7080],
        ["gateway", 5212],
        ["identity-service", 5062],
        ["organization-service", 8081],
        ["compliance-service", 5001],
        ["audit-service", 5072],
        ["ai", 5080],
        ["knowledge-base", 5090],
        ["portal", 4200],
        ["outbox-worker", 0]
    ];

    [Theory]
    [MemberData(nameof(PlatformServices))]
    public void InfrastructureContractBuilder_CoversEveryDeployableService(string serviceId, int port)
    {
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            ImageReference = $"aovias/{serviceId}:1.0.0",
            ContainerName = serviceId,
            EntryPoint = "App.dll",
            HealthEndpoint = "/health",
            Ports = port > 0 ? [port] : [],
            Dependencies = ResolveDependencies(serviceId)
        });

        var contract = InfrastructureContractBuilder.Build(bundle);

        Assert.Equal(serviceId, contract.ServiceId);
        Assert.NotNull(contract.Docker);
        Assert.NotNull(contract.Network);
        Assert.NotNull(contract.Health);
        Assert.Equal($"aovias/{serviceId}:1.0.0", contract.Docker.ImageReference);
    }

    [Fact]
    public async Task Pipeline_GeneratesReadinessReportForAllPlatformServices()
    {
        var configRoot = CreateMinimalConfigRoot();

        foreach (var serviceId in PlatformServices.Select(item => (string)item[0]))
        {
            var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
            {
                ServiceId = serviceId,
                DisplayName = serviceId,
                Version = "1.0.0",
                ImageReference = $"aovias/{serviceId}:1.0.0",
                ContainerName = serviceId,
                EntryPoint = "App.dll",
                HealthEndpoint = "/health",
                Ports = [8080],
                Dependencies = ResolveDependencies(serviceId)
            });

            var report = await InfrastructureProvisioningPipeline.ExecuteAsync(new InfrastructureProvisioningContext
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
            });

            Assert.NotEmpty(report.Stages);
            Assert.Contains(report.Stages, stage => stage.Stage == "Validation");
        }

        Directory.Delete(configRoot, recursive: true);
    }

    private static string CreateMinimalConfigRoot()
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var secretsDirectory = Path.Combine(configRoot, "secrets", "local");
        Directory.CreateDirectory(secretsDirectory);
        Directory.CreateDirectory(Path.Combine(configRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(configRoot, "packages"));
        File.WriteAllText(
            Path.Combine(secretsDirectory, "settings.secrets.json"),
            """
            {
              "jwtSigningKey": "test-jwt",
              "jwtIssuer": "test-issuer",
              "jwtAudience": "test-audience",
              "databaseConnectionString": "Server=.;Database=Test;Trusted_Connection=True;",
              "certificatePassword": "test-password",
              "RabbitMQ:HostName": "rabbitmq",
              "RabbitMQ:Password": "rabbitmq-password",
              "ConnectionStrings:ServiceBus": "Endpoint=sb://test.servicebus.windows.net/;"
            }
            """);
        return configRoot;
    }

    private static IReadOnlyList<string> ResolveDependencies(string serviceId)
        => serviceId switch
        {
            "gateway" => ["runtime-agent"],
            "identity-service" => ["gateway"],
            "organization-service" => ["gateway", "identity-service"],
            "compliance-service" => ["gateway", "organization-service"],
            "audit-service" => ["gateway", "compliance-service"],
            "ai" => ["gateway", "compliance-service"],
            "knowledge-base" => ["gateway"],
            "portal" => ["gateway"],
            "outbox-worker" => ["compliance-service"],
            _ => []
        };
}
