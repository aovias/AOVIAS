namespace Aovias.Runtime.Contracts.Runtime;

/// <summary>
/// Describes runtime and platform version metadata.
/// </summary>
public sealed class RuntimeVersionDto
{
    /// <summary>
    /// Gets or sets the runtime agent version.
    /// </summary>
    public string RuntimeVersion { get; init; } = "1.0";

    /// <summary>
    /// Gets or sets the AOVIAS platform version.
    /// </summary>
    public string PlatformVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment agent version.
    /// </summary>
    public string DeploymentAgentVersion { get; init; } = string.Empty;
}
