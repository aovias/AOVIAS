namespace Aovias.Runtime.Contracts.Deployment;

/// <summary>
/// Runtime Agent deployment API availability and discovery metadata.
/// </summary>
public sealed class RuntimeDeploymentAvailabilityDto
{
    public bool Available { get; init; }

    public string ApiVersion { get; init; } = string.Empty;

    public int PathPolicyVersion { get; init; }

    public bool PathPolicyCompliant { get; init; }

    public RuntimeSignalRDiscoveryDto? SignalR { get; init; }
}

/// <summary>
/// SignalR hub discovery metadata exposed by the Runtime Agent.
/// </summary>
public sealed class RuntimeSignalRDiscoveryDto
{
    public bool Enabled { get; init; }

    public string Hub { get; init; } = string.Empty;

    public string HubUrl { get; init; } = string.Empty;

    public string NegotiatePath { get; init; } = string.Empty;

    public IReadOnlyList<string> Protocols { get; init; } = [];
}
