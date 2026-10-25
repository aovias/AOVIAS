using Aovias.Configuration.Deployment;
using Aovias.Configuration.Features;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aovias.Configuration.Tests;

public sealed class PlatformFeatureActivationTests
{
    [Fact]
    public void Resolve_UsesExplicitFeatureFlagsWhenPresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:Runtime"] = "true",
                ["Features:ReleaseManagement"] = "true",
                ["Features:DockerProvider"] = "true",
                ["Features:SignalR"] = "true",
                ["Features:Publishing"] = "false",
                ["Features:Certificates"] = "false",
                ["Features:Monitoring"] = "false",
                ["Features:Observability"] = "false"
            })
            .Build();

        var features = PlatformFeatureActivation.Resolve(configuration);

        Assert.True(features.Runtime);
        Assert.True(features.ReleaseManagement);
        Assert.True(features.DockerProvider);
        Assert.True(features.SignalR);
        Assert.False(features.Publishing);
        Assert.False(features.Certificates);
        Assert.False(features.Monitoring);
        Assert.False(features.Observability);
        Assert.False(features.RequiresSecretsValidation);
    }

    [Fact]
    public void GetReachableSections_SkipsDisabledModules()
    {
        var features = new PlatformFeatureActivation
        {
            Runtime = true,
            ReleaseManagement = true,
            DockerProvider = true,
            SignalR = true
        };

        var sections = ConfigurationDependencyGraph.GetReachableSections(features);

        Assert.Contains(RuntimeSettings.SectionName, sections);
        Assert.Contains(ReleaseManagementSettings.SectionName, sections);
        Assert.Contains(DockerSettings.SectionName, sections);
        Assert.Contains(SignalRSettings.SectionName, sections);
        Assert.DoesNotContain(PublishingSettings.SectionName, sections);
        Assert.DoesNotContain(CertificateSettings.SectionName, sections);
        Assert.DoesNotContain(MonitoringSettings.SectionName, sections);
        Assert.DoesNotContain(ObservabilitySettings.SectionName, sections);
    }

    [Fact]
    public void DeploymentHostPathsResolver_RejectsPlaceholders()
    {
        var publishingSettings = new PublishingSettings
        {
            ValidationConfigRoot = Path.GetTempPath(),
            DeploymentActiveProfile = "invalid",
            DefaultNetworkName = "aovias-network"
        };

        var profileDirectory = Path.Combine(publishingSettings.ValidationConfigRoot, "profiles", publishingSettings.DeploymentActiveProfile);
        Directory.CreateDirectory(profileDirectory);
        var settingsFile = Path.Combine(profileDirectory, "settings.deployment-host.json");
        File.WriteAllText(settingsFile, """
                                        {
                                          "dotNetEnvironment": "Production",
                                          "configRoot": "/path/to/config",
                                          "packagesRoot": "/opt/aovias/runtime/packages",
                                          "logsRoot": "/opt/aovias/runtime/logs",
                                          "hostConfigRoot": "/opt/aovias/runtime/config",
                                          "hostPackagesRoot": "/opt/aovias/runtime/packages",
                                          "hostLogsRoot": "/opt/aovias/runtime/logs",
                                          "dockerNetwork": "aovias-network"
                                        }
                                        """);

        try
        {
            Assert.Throws<InvalidOperationException>(() => DeploymentHostPathsResolver.Resolve(publishingSettings));
        }
        finally
        {
            File.Delete(settingsFile);
            Directory.Delete(profileDirectory, recursive: true);
        }
    }
}
