namespace Aovias.Runtime.Agent.Domain.ValueObjects;

/// <summary>
/// Represents a semantic runtime version value.
/// </summary>
public sealed record RuntimeVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeVersion"/> record.
    /// </summary>
    /// <param name="value">The version string.</param>
    public RuntimeVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the canonical runtime version for Phase 2.
    /// </summary>
    public static RuntimeVersion Current { get; } = new("1.0");

    /// <summary>
    /// Gets the version string.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
