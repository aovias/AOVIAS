using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aovias.Publishing.Core.Execution;

/// <summary>
/// Default process runner for CLI tools.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> RunAsync(
        string executable,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        logger.LogInformation("Executing: {Executable} {Arguments} in {WorkingDirectory}", executable, arguments, workingDirectory);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new ProcessExecutionResult
            {
                ExitCode = -1,
                StandardError = $"Unable to start '{executable}': {ex.Message}"
            };
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort.
            }

            return new ProcessExecutionResult
            {
                ExitCode = -1,
                StandardError = $"Process timed out after {timeoutSeconds} seconds."
            };
        }

        return new ProcessExecutionResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = await stdoutTask,
            StandardError = await stderrTask
        };
    }
}
