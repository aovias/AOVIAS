using System.Diagnostics;
using Aovias.DeploymentValidation.Models;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Executes external validation commands.
/// </summary>
public interface IValidationCommandRunner
{
    Task<ValidationCommandResult> RunAsync(
        string executable,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        DeploymentValidationContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Command execution result.
/// </summary>
public sealed record ValidationCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
}

/// <summary>
/// Default process runner for deployment validation.
/// </summary>
public sealed class ValidationCommandRunner : IValidationCommandRunner
{
    private readonly ILogger<ValidationCommandRunner> logger;

    public ValidationCommandRunner(ILogger<ValidationCommandRunner> logger)
    {
        this.logger = logger;
    }

    public async Task<ValidationCommandResult> RunAsync(
        string executable,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var commandLine = $"{executable} {arguments}";
        context.CommandLog.Add(commandLine);
        logger.LogInformation("DRV executing: {Command} in {Directory}", commandLine, workingDirectory);

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

        using var process = new Process { StartInfo = startInfo };
        process.Start();

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

            return new ValidationCommandResult(-1, string.Empty, $"Command timed out after {timeoutSeconds} seconds.");
        }

        return new ValidationCommandResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }
}
