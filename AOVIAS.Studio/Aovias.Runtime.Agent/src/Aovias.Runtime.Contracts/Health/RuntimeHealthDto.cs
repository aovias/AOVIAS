namespace Aovias.Runtime.Contracts.Health;

/// <summary>
/// Represents the overall health snapshot of the runtime control plane.
/// </summary>
public sealed class RuntimeHealthDto
{
    /// <summary>
    /// Gets or sets the overall status label.
    /// </summary>
    public string Status { get; init; } = "Healthy";

    /// <summary>
    /// Gets or sets the health summary message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets when the health snapshot was evaluated.
    /// </summary>
    public DateTimeOffset CheckedAt { get; init; }

    /// <summary>
    /// Gets or sets component-level health entries.
    /// </summary>
    public IReadOnlyList<RuntimeComponentHealthDto> Components { get; init; } = [];
}

/// <summary>
/// Represents health for an individual runtime component.
/// </summary>
public sealed class RuntimeComponentHealthDto
{
    /// <summary>
    /// Gets or sets the component name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the component status label.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the component health label.
    /// </summary>
    public string Health { get; init; } = string.Empty;
}
