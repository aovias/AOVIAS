namespace Aovias.Publishing.Core.Execution;

/// <summary>
/// Result of an external process invocation.
/// </summary>
public sealed class ProcessExecutionResult
{
    public int ExitCode { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public bool IsSuccess => ExitCode == 0;
}

/// <summary>
/// Executes external processes using configured executables.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessExecutionResult> RunAsync(
        string executable,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken);
}
