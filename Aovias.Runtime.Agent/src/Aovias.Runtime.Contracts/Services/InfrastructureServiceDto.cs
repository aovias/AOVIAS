namespace Aovias.Runtime.Contracts.Services;

/// <summary>
/// Describes an infrastructure service managed by the runtime control plane.
/// </summary>
public sealed class InfrastructureServiceDto
{
    /// <summary>
    /// Gets or sets the service identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the service category.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployed version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the operational status label.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the health label.
    /// </summary>
    public string Health { get; init; } = string.Empty;
}
