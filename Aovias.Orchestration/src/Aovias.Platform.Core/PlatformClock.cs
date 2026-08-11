namespace Aovias.Platform.Core;

/// <summary>
/// Abstraction over system clock for testability.
/// </summary>
public interface IPlatformClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// System clock implementation.
/// </summary>
public sealed class PlatformClock : IPlatformClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
