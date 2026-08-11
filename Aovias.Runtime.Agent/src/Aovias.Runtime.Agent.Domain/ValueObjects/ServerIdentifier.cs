namespace Aovias.Runtime.Agent.Domain.ValueObjects;

/// <summary>
/// Represents a unique server identifier within the AOVIAS platform.
/// </summary>
public sealed record ServerIdentifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerIdentifier"/> record.
    /// </summary>
    /// <param name="value">The server identifier value.</param>
    public ServerIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the identifier value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
