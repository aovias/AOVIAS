using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Writes deployment validation diagnostics to a log file.
/// </summary>
public sealed class DeploymentValidationLogger
{
    private readonly ILogger<DeploymentValidationLogger> logger;
    private readonly object sync = new();
    private StreamWriter? writer;

    public DeploymentValidationLogger(ILogger<DeploymentValidationLogger> logger)
    {
        this.logger = logger;
    }

    public string? LogFilePath { get; private set; }

    public void Begin(string workingDirectory)
    {
        Directory.CreateDirectory(workingDirectory);
        LogFilePath = Path.Combine(workingDirectory, "DeploymentValidation.log");
        writer = new StreamWriter(LogFilePath, append: false) { AutoFlush = true };
        WriteLine("Deployment Readiness Validation started.");
    }

    public void WriteStage(string stageName, ValidationStageResult result)
    {
        WriteLine($"[{stageName}] Passed={result.Passed} Duration={result.Duration.TotalSeconds:F2}s");
        foreach (var error in result.Errors)
        {
            WriteLine($"  ERROR: {error}");
        }

        foreach (var warning in result.Warnings)
        {
            WriteLine($"  WARN: {warning}");
        }
    }

    public void WriteCommand(string commandLine, ValidationCommandResult result)
    {
        WriteLine($"$ {commandLine}");
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            WriteLine(result.StandardOutput.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            WriteLine(result.StandardError.Trim());
        }
    }

    public void WriteHttp(string endpoint, int statusCode, string body)
        => WriteLine($"HTTP {endpoint} -> {statusCode}: {body.Trim()}");

    public void Complete(DeploymentReadinessReport report)
    {
        WriteLine($"Validation completed. Ready={report.IsReady} Score={report.OverallScore}% Duration={report.ValidationDuration.TotalSeconds:F2}s");
        writer?.Dispose();
        writer = null;
    }

    private void WriteLine(string message)
    {
        var line = $"{DateTimeOffset.UtcNow:u} {message}";
        logger.LogInformation("{ValidationLog}", line);
        lock (sync)
        {
            writer?.WriteLine(line);
        }
    }
}
