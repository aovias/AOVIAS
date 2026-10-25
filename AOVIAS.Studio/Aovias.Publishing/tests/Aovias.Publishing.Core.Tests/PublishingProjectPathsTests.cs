using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Discovery;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class PublishingProjectPathsTests
{
    [Fact]
    public void Exists_accepts_angular_project_file_path()
    {
        var webAppRoot = Path.Combine(
            PlatformRootResolver.Resolve(null),
            "AOVIAS.Command",
            "WebApp");

        var angularJson = Path.Combine(webAppRoot, "angular.json");
        if (!File.Exists(angularJson))
        {
            return;
        }

        Assert.True(PublishingProjectPaths.Exists(angularJson));
    }

    [Fact]
    public void DeployableProject_uses_file_path_for_portal_publish_request()
    {
        var webAppRoot = Path.Combine(
            PlatformRootResolver.Resolve(null),
            "AOVIAS.Command",
            "WebApp");
        var angularJson = Path.Combine(webAppRoot, "angular.json");
        if (!File.Exists(angularJson))
        {
            return;
        }

        var project = new DeployableProject
        {
            Name = "Angular Portal",
            ServiceId = "portal",
            ProjectPath = angularJson,
            DiscoveryPath = webAppRoot,
            Type = PublishingProjectType.Angular,
            OutputPath = "packages",
            Version = "1.0.0",
            StartupOrder = 100,
            ContainerPort = 80,
            HealthEndpoint = "/",
            DependsOn = ["gateway"],
            ReleaseLibraryCategory = "frontend"
        };

        Assert.True(PublishingProjectPaths.Exists(project.ProjectPath));
        Assert.False(File.Exists(project.DiscoveryPath));
        Assert.True(Directory.Exists(project.DiscoveryPath));
    }
}
