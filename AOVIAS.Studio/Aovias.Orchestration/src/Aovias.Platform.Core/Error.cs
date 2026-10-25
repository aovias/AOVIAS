namespace Aovias.Platform.Core;

/// <summary>
/// Structured platform error.
/// </summary>
public sealed class Error
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? Detail { get; init; }

    public static Error Create(string code, string message, string? detail = null)
        => new() { Code = code, Message = message, Detail = detail };
}
