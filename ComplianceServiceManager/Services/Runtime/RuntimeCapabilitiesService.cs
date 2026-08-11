using Aovias.Orchestration.Contracts.Capabilities;
using Aovias.Runtime.SDK;

namespace ComplianceServiceManager.Services.Runtime;

/// <summary>
/// Loads runtime capabilities from the connected agent.
/// </summary>
public interface IRuntimeCapabilitiesService
{
    IReadOnlyList<string> Capabilities { get; }

    bool Supports(string capability);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Runtime capabilities service backed by the runtime SDK.
/// </summary>
public sealed class RuntimeCapabilitiesService : IRuntimeCapabilitiesService
{
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private IReadOnlyList<string> capabilities = [];

    public RuntimeCapabilitiesService(IRuntimeAgentClient runtimeAgentClient)
    {
        this.runtimeAgentClient = runtimeAgentClient;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Capabilities => capabilities;

    /// <inheritdoc />
    public bool Supports(string capability)
        => capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var result = await runtimeAgentClient.GetRuntimeCapabilitiesAsync(cancellationToken);
        capabilities = result.Capabilities;
    }
}
