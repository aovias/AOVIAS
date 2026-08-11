using Aovias.Configuration.Features;
using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Capabilities;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// Runtime capabilities provider.
/// </summary>
public sealed class RuntimeCapabilitiesProvider
{
    private readonly PlatformFeatureActivation features;
    private readonly IOptionsMonitor<SignalRSettings> signalRSettings;
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;

    public RuntimeCapabilitiesProvider(
        PlatformFeatureActivation features,
        IOptionsMonitor<SignalRSettings> signalRSettings,
        IOptionsMonitor<ProviderSettings> providerSettings)
    {
        this.features = features;
        this.signalRSettings = signalRSettings;
        this.providerSettings = providerSettings;
    }

    public RuntimeCapabilities GetCapabilities(string runtimeVersion)
    {
        var capabilities = new List<string>();

        if (features.Runtime)
        {
            capabilities.Add(RuntimeCapability.Runtime);
            capabilities.Add(RuntimeCapability.HealthMonitoring);
        }

        if (features.ReleaseManagement)
        {
            capabilities.Add(RuntimeCapability.ReleaseManagement);
            capabilities.Add(RuntimeCapability.WorkflowEngine);
            capabilities.Add(RuntimeCapability.Rollback);
            capabilities.Add(RuntimeCapability.RemoteDeploymentOrchestration);
            capabilities.Add(RuntimeCapability.PlatformCertification);
        }

        if (features.DockerProvider && providerSettings.CurrentValue.DockerEnabled)
        {
            capabilities.Add(RuntimeCapability.DockerProvider);
        }

        if (features.SignalR && signalRSettings.CurrentValue.Enabled)
        {
            capabilities.Add(RuntimeCapability.SignalR);
        }

        if (features.Publishing)
        {
            capabilities.Add(RuntimeCapability.Publishing);
        }

        if (features.Monitoring)
        {
            capabilities.Add(RuntimeCapability.Monitoring);
        }

        if (features.Certificates)
        {
            capabilities.Add(RuntimeCapability.CertificateManagement);
        }

        if (features.Observability)
        {
            capabilities.Add(RuntimeCapability.Observability);
        }

        return new RuntimeCapabilities
        {
            RuntimeVersion = runtimeVersion,
            Capabilities = capabilities
        };
    }
}
