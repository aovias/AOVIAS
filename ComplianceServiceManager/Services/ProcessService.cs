using System.Diagnostics;
using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class ProcessService
{
    private readonly ILoggingService logger;

    public ProcessService(ILoggingService logger)
    {
        this.logger = logger;
    }

    public async Task<ProcessResult> RunCommandAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string source,
        LogLevel outputLevel,
        CancellationToken cancellationToken)
    {
        using var process = CreateProcess(fileName, arguments, workingDirectory);
        process.EnableRaisingEvents = true;

        logger.Process(source, $"{fileName} {arguments}");

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Unable to start process: {fileName}");
            }

            var standardOutputTask = PumpReaderAsync(
                process.StandardOutput,
                line => LogLine(line, source, outputLevel),
                cancellationToken);
            var standardErrorTask = PumpReaderAsync(
                process.StandardError,
                line => LogLine(line, source, outputLevel == LogLevel.Build ? LogLevel.Build : LogLevel.Error),
                cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            return new ProcessResult { ExitCode = process.ExitCode };
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

    public Process StartLongRunning(
        string fileName,
        string arguments,
        string workingDirectory,
        string source,
        IReadOnlyDictionary<string, string>? environmentVariables,
        Action<string> onStandardOutput,
        Action<string> onErrorOutput)
    {
        var process = CreateProcess(fileName, arguments, workingDirectory);
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                logger.Process(source, args.Data);
                onStandardOutput(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                logger.Error(source, args.Data);
                onErrorOutput(args.Data);
            }
        };

        logger.Process(source, $"{fileName} {arguments}");

        if (!process.Start())
        {
            throw new InvalidOperationException($"Unable to start process: {fileName}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    public async Task StopAsync(Process? process, string source, CancellationToken cancellationToken)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (process.HasExited)
            {
                return;
            }

            logger.Process(source, $"Stopping PID {process.Id}.");
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(cancellationToken);
            logger.Process(source, "Process stopped.");
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception ex)
        {
            logger.Error(source, $"Stop failed: {ex.Message}");
            throw;
        }
        finally
        {
            process.Dispose();
        }
    }

    private static Process CreateProcess(string fileName, string arguments, string workingDirectory)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
    }

    private void LogLine(string? data, string source, LogLevel level)
    {
        if (!string.IsNullOrWhiteSpace(data))
        {
            logger.Log(level, source, data);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    private static async Task PumpReaderAsync(StreamReader reader, Action<string> onLine, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                onLine(line);
            }
        }
    }
}
