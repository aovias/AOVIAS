namespace Aovias.Platform.Core;

/// <summary>
/// Strongly typed operation identifier for platform workflows.
/// </summary>
public readonly record struct OperationId(string Value)
{
    /// <summary>
    /// Creates a new operation identifier.
    /// </summary>
    public static OperationId New() => new($"OP-{Guid.NewGuid():N}");

    /// <inheritdoc />
    public override string ToString() => Value;
}
