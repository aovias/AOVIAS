using System.Text.Json;
using System.Text.RegularExpressions;
using Aovias.Configuration.SignalR;
using Aovias.Runtime.Contracts.Deployment;
using Xunit;

namespace Aovias.DeploymentValidation.Tests.Reliability;

/// <summary>
/// Sprint 11.1 SignalR routing and discovery regression suite.
/// </summary>
public sealed class SignalRRegressionTests
{
    private static readonly string[] ForbiddenClientPatterns =
    [
        @"WithUrl\(\s*""https?://",
        @"WithUrl\(\s*""/hubs/",
        @"WithUrl\(\s*""/api/runtime",
        @"""deploymentHub""",
        @"""\/deploymentHub"""
    ];

    [Fact]
    public void Runtime_agent_maps_single_runtime_hub()
    {
        var program = ReadAgentApiSource("Program.cs");
        Assert.Contains("MapHub<RuntimeHub>", program, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Count(program, "MapHub<", RegexOptions.CultureInvariant));
    }

    [Fact]
    public void Deployment_status_contract_includes_signalr_discovery()
    {
        var controller = ReadAgentApiSource("Controllers", "DeploymentAvailabilityController.cs");
        Assert.Contains("RuntimeSignalRDiscoveryDto", controller, StringComparison.Ordinal);
        Assert.Contains("RuntimeDeploymentAvailabilityDto", controller, StringComparison.Ordinal);
        Assert.Contains("SignalRHubEndpointResolver", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_hub_client_discovers_endpoint_before_connecting()
    {
        var client = ReadSdkSource("RuntimeHubClient.cs");
        Assert.Contains("GetDeploymentAvailabilityAsync", client, StringComparison.Ordinal);
        Assert.Contains("SignalRHubEndpointResolver", client, StringComparison.Ordinal);
        Assert.DoesNotContain("new Uri(new Uri(apiSettings.CurrentValue.BaseUrl), settings.HubPath)", client, StringComparison.Ordinal);
    }

    [Fact]
    public void SignalR_discovery_roundtrip_serializes_expected_shape()
    {
        var payload = new RuntimeDeploymentAvailabilityDto
        {
            Available = true,
            ApiVersion = "sprint-11",
            SignalR = new RuntimeSignalRDiscoveryDto
            {
                Enabled = true,
                Hub = "/hubs/deployment",
                HubUrl = "http://127.0.0.1:7080/hubs/deployment",
                NegotiatePath = "/hubs/deployment/negotiate?negotiateVersion=1",
                Protocols = SignalRHubEndpointResolver.SupportedProtocols
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        using var document = JsonDocument.Parse(json);
        var signalR = document.RootElement.GetProperty("signalR");
        Assert.Equal("/hubs/deployment", signalR.GetProperty("hub").GetString());
        Assert.Contains("WebSockets", signalR.GetProperty("protocols").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void Command_and_sdk_must_not_hardcode_signalr_urls()
    {
        var violations = new List<string>();
        ScanForForbiddenPatterns(FindRepositoryRoot("ComplianceServiceManager"), violations);
        ScanForForbiddenPatterns(FindRepositoryRoot("Aovias.Runtime.Agent", "src", "Aovias.Runtime.SDK"), violations);

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static void ScanForForbiddenPatterns(string root, ICollection<string> violations)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            foreach (var pattern in ForbiddenClientPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    violations.Add($"{Path.GetRelativePath(FindRepositoryRoot(), file)} matches `{pattern}`");
                }
            }
        }
    }

    private static string ReadAgentApiSource(params string[] segments)
        => File.ReadAllText(Path.Combine(FindRepositoryRoot("Aovias.Runtime.Agent", "src", "Aovias.Runtime.Agent.Api"), Path.Combine(segments)));

    private static string ReadSdkSource(string fileName)
        => File.ReadAllText(Path.Combine(FindRepositoryRoot("Aovias.Runtime.Agent", "src", "Aovias.Runtime.SDK"), fileName));

    private static string FindRepositoryRoot(params string[] suffix)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (suffix.Length == 0)
            {
                return current.FullName;
            }

            var candidate = Path.Combine(current.FullName, Path.Combine(suffix));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException($"Could not locate repository path '{Path.Combine(suffix)}'.");
    }
}
