using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.Tests.WorkspaceRuntime;

public sealed class ServiceLaunchCommandTests
{
    [Fact]
    public void WorkspaceHttps_InjectsRegisteredPort_AndSuppressesLaunchSettings()
    {
        var service = new ServiceInfoModel
        {
            Name = "OrganizationService",
            Kind = ServiceKind.WebApi,
            ProjectPath = @"V:\workspace\OrganizationService\OrganizationService.csproj",
            LaunchProfileName = null,
            SuppressLaunchSettings = true,
            HttpsUrl = "https://localhost:5101",
            HttpsPort = 5101,
            Port = 5101,
            LaunchEnvironmentVariables = new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = "http://localhost:5085"
            }
        };

        var (fileName, arguments) = ServiceLaunchCommand.BuildStartCommand(service);
        var environment = ServiceLaunchCommand.BuildEnvironmentVariables(service);

        Assert.Equal("dotnet", fileName);
        Assert.Contains("--no-launch-profile", arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--launch-profile", arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("5085", arguments, StringComparison.Ordinal);
        Assert.Equal("https://localhost:5101", environment["ASPNETCORE_URLS"]);
        Assert.DoesNotContain("5085", environment["ASPNETCORE_URLS"]);
    }

    [Fact]
    public void WorkspaceHttp_InjectsRegisteredPort_AndSuppressesLaunchSettings()
    {
        var service = new ServiceInfoModel
        {
            Name = "OrganizationService",
            Kind = ServiceKind.WebApi,
            ProjectPath = @"V:\workspace\OrganizationService\OrganizationService.csproj",
            LaunchProfileName = "http",
            SuppressLaunchSettings = true,
            HttpUrl = "http://localhost:5101",
            HttpPort = 5101,
            Port = 5101
        };

        var (_, arguments) = ServiceLaunchCommand.BuildStartCommand(service);
        var environment = ServiceLaunchCommand.BuildEnvironmentVariables(service);

        Assert.Contains("--no-launch-profile", arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--launch-profile", arguments, StringComparison.Ordinal);
        Assert.Equal("http://localhost:5101", environment["ASPNETCORE_URLS"]);
        Assert.DoesNotContain("5085", environment["ASPNETCORE_URLS"]);
    }

    [Fact]
    public void AngularWorkspace_BindsRegisteredHttpPort()
    {
        var service = new ServiceInfoModel
        {
            Name = "WebApp",
            Kind = ServiceKind.AngularWebApp,
            Port = 4201,
            HttpPort = 4201,
            SuppressLaunchSettings = true
        };

        var (fileName, arguments) = ServiceLaunchCommand.BuildStartCommand(service);
        Assert.Equal("cmd.exe", fileName);
        Assert.Contains("--port 4201", arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("4200", arguments, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedRuntime_KeepsLaunchProfile_AndDoesNotForceNoLaunchProfile()
    {
        var service = new ServiceInfoModel
        {
            Name = "OrganizationService",
            Kind = ServiceKind.WebApi,
            ProjectPath = @"V:\AOVIAS\OrganizationService\OrganizationService.csproj",
            LaunchProfileName = "https",
            SuppressLaunchSettings = false,
            HttpsUrl = "https://localhost:7217",
            HttpsPort = 7217
        };

        var (_, arguments) = ServiceLaunchCommand.BuildStartCommand(service);
        var environment = ServiceLaunchCommand.BuildEnvironmentVariables(service);

        Assert.Contains("--launch-profile \"https\"", arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--no-launch-profile", arguments, StringComparison.Ordinal);
        Assert.False(environment.ContainsKey("ASPNETCORE_URLS"));
    }
}
