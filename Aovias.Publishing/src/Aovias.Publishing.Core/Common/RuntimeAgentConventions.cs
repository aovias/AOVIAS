namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Canonical conventions for identifying the AOVIAS Runtime Agent service.
/// Centralizes the detection logic that was previously duplicated across the
/// discovery provider, sanitizer, compose generator and Docker context stager.
/// </summary>
public static class RuntimeAgentConventions
{
    /// <summary>
    /// Stable service identifier assigned to the Runtime Agent.
    /// </summary>
    public const string ServiceId = "runtime-agent";

    /// <summary>
    /// Default container port the Runtime Agent listens on.
    /// </summary>
    public const int DefaultPort = 7080;

    /// <summary>
    /// Runtime Agent API entry-point assembly.
    /// </summary>
    public const string EntryPointAssembly = "Aovias.Runtime.Agent.Api.dll";

    private const string ProjectMarker = "Aovias.Runtime.Agent";

    /// <summary>
    /// Determines whether the supplied service id or any of the supplied paths
    /// identify the Runtime Agent.
    /// </summary>
    public static bool IsRuntimeAgent(string? serviceId, params string?[] paths)
    {
        if (string.Equals(serviceId, ServiceId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var path in paths)
        {
            if (!string.IsNullOrEmpty(path) && path.Contains(ProjectMarker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
