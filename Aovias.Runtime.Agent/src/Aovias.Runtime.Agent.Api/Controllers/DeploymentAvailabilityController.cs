using System.Text.Json;
using Aovias.Configuration.Features;
using Aovias.Configuration.Settings;
using Aovias.Configuration.SignalR;
using Aovias.Runtime.Contracts.Deployment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Api.Controllers;

/// <summary>
/// Lightweight deployment API probe that does not depend on deployment persistence services.
/// </summary>
[ApiController]
[Route("api/deployment")]
[Produces("application/json")]
public sealed class DeploymentAvailabilityController : ControllerBase
{
    private const int RequiredPathPolicyVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly IOptionsMonitor<SignalRSettings> signalRSettings;
    private readonly PlatformFeatureActivation features;

    public DeploymentAvailabilityController(
        IOptionsMonitor<ApiSettings> apiSettings,
        IOptionsMonitor<SignalRSettings> signalRSettings,
        PlatformFeatureActivation features)
    {
        this.apiSettings = apiSettings;
        this.signalRSettings = signalRSettings;
        this.features = features;
    }

    [HttpGet("status")]
    public IActionResult GetDeploymentApiStatus()
    {
        var manifest = TryReadBuildManifest();
        var pathPolicyVersion = manifest?.PathPolicyVersion ?? 0;
        var api = apiSettings.CurrentValue;
        var signalR = signalRSettings.CurrentValue;
        var signalREnabled = features.SignalR && signalR.Enabled;

        RuntimeSignalRDiscoveryDto? signalRDiscovery = null;
        if (signalREnabled)
        {
            var requestPathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
            var hubPath = SignalRHubEndpointResolver.ResolveHubPath(requestPathBase, signalR.HubPath);
            signalRDiscovery = new RuntimeSignalRDiscoveryDto
            {
                Enabled = true,
                Hub = hubPath,
                HubUrl = $"{Request.Scheme}://{Request.Host}{hubPath}",
                NegotiatePath = $"{hubPath.TrimEnd('/')}/negotiate?negotiateVersion=1",
                Protocols = SignalRHubEndpointResolver.SupportedProtocols
            };
        }

        return Ok(new RuntimeDeploymentAvailabilityDto
        {
            Available = true,
            ApiVersion = "sprint-11",
            PathPolicyVersion = pathPolicyVersion,
            PathPolicyCompliant = pathPolicyVersion >= RequiredPathPolicyVersion,
            SignalR = signalRDiscovery
        });
    }

    private static RuntimeAgentBuildManifest? TryReadBuildManifest()
    {
        const string manifestPath = "/app/aovias-build-manifest.json";
        if (!System.IO.File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RuntimeAgentBuildManifest>(
                System.IO.File.ReadAllText(manifestPath),
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class RuntimeAgentBuildManifest
    {
        public string SourceRevision { get; init; } = string.Empty;

        public string BuildUtc { get; init; } = string.Empty;

        public int PathPolicyVersion { get; init; }
    }
}
