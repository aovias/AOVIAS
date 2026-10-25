namespace Aovias.Platform.Core;

/// <summary>
/// Result of executing a pipeline or workflow step.
/// </summary>
public sealed class ExecutionResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public static ExecutionResult Success(string message = "Completed.")
        => new() { IsSuccess = true, Message = message };

    public static ExecutionResult Failure(string message)
        => new() { IsSuccess = false, Message = message };
}
