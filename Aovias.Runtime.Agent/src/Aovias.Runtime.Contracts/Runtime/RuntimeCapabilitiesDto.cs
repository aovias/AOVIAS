namespace Aovias.Runtime.Contracts.Runtime;

/// <summary>
/// Runtime capability advertisement returned by the control plane.
/// </summary>
public sealed class RuntimeCapabilitiesDto
{
    public required string RuntimeVersion { get; init; }

    public required IReadOnlyList<string> Capabilities { get; init; }
}
