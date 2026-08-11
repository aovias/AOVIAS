namespace ComplianceServiceManager.Models.Runtime;

/// <summary>
/// Represents a managed service running within the AOVIAS Runtime stack.
/// </summary>
public sealed class RuntimeService
{
    /// <summary>
    /// Gets or sets the unique service identifier.
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
    /// Gets or sets the deployed service version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the operational status label.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the health classification.
    /// </summary>
    public RuntimeHealth Health { get; init; } = RuntimeHealth.Unknown;

    /// <summary>
    /// Gets or sets when the service was started.
    /// </summary>
    public DateTimeOffset Started { get; init; }

    /// <summary>
    /// Gets or sets the formatted uptime label.
    /// </summary>
    public string Uptime { get; init; } = string.Empty;
}
