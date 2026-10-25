using System.Text;
using System.Text.Json;
using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.Diagnostics;

public sealed class DiagnosticsCollector : IDiagnosticsCollector
{
    private readonly ILoggingService logger;
    private readonly ProcessService processService;
    private readonly ILogFileService logFileService;
    private readonly IFixedErrorStore fixedErrorStore;

    public DiagnosticsCollector(
        ILoggingService logger,
        ProcessService processService,
        ILogFileService logFileService,
        IFixedErrorStore fixedErrorStore)
    {
        this.logger = logger;
        this.processService = processService;
        this.logFileService = logFileService;
        this.fixedErrorStore = fixedErrorStore;
    }

    public async Task<DiagnosticBundle> CollectAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        var sessionId = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var sessionRoot = Path.Combine(AppContext.BaseDirectory, "Diagnostics", sessionId, service.Name);
        Directory.CreateDirectory(sessionRoot);

        var logsPath = Path.Combine(sessionRoot, "logs.txt");
        var envPath = Path.Combine(sessionRoot, "environment.json");
        var gitDiffPath = Path.Combine(sessionRoot, "git-diff.txt");
        var healthPath = Path.Combine(sessionRoot, "health.json");
        var summaryPath = Path.Combine(sessionRoot, "summary.md");
        var promptPath = Path.Combine(sessionRoot, "cursor-prompt.md");

        await CaptureLogsAsync(service, logsPath, cancellationToken);
        await CaptureEnvironmentAsync(service, envPath, cancellationToken);
        await CaptureGitAsync(service, gitDiffPath, cancellationToken);
        await CaptureHealthAsync(service, healthPath, cancellationToken);

        var summaryText = await File.ReadAllTextAsync(logsPath, cancellationToken);

        // Split structured failures into those already resolved in a previous fix session and
        // those that still need fixing. "New" failures are unknown fingerprints OR failures that
        // recurred after the last fix timestamp (meaning the previous fix did not hold).
        var lastFixedAt = fixedErrorStore.GetLastFixedAt(service.Name);
        var resolvedFingerprints = fixedErrorStore.GetResolvedFingerprints(service.Name);
        var failures = await logFileService.GetLatestFailuresAsync(service.Name, 40, cancellationToken);

        var unresolvedFailures = failures
            .Where(f => !resolvedFingerprints.Contains(f.Fingerprint)
                        || (lastFixedAt is not null && f.Timestamp > lastFixedAt))
            .OrderByDescending(f => f.Timestamp)
            .ToList();
        var previouslyFixedFailures = failures
            .Where(f => resolvedFingerprints.Contains(f.Fingerprint))
            .OrderByDescending(f => f.Timestamp)
            .ToList();

        // Prefer deterministic EventCode classification from the unresolved structured failures;
        // fall back to string matching when no coded failure is present.
        var codedCategory = unresolvedFailures
            .Select(f => MapEventCode(f.EventCode))
            .FirstOrDefault(c => c != FailureCategory.Unknown);
        var category = codedCategory != FailureCategory.Unknown
            ? codedCategory
            : Classify(summaryText, service.LastAction);
        var summary = BuildSummary(service, category, summaryText);
        await File.WriteAllTextAsync(summaryPath, summary, cancellationToken);

        return new DiagnosticBundle
        {
            SessionId = sessionId,
            RootDirectory = sessionRoot,
            ServiceName = service.Name,
            ServiceDirectory = service.WorkingDirectory,
            PromptPath = promptPath,
            SummaryPath = summaryPath,
            LogsPath = logsPath,
            EnvironmentPath = envPath,
            GitDiffPath = gitDiffPath,
            HealthPath = healthPath,
            FailureCategory = category,
            FailureSummary = summary,
            PrimaryLogFile = logFileService.ListLogFiles(service.Name).FirstOrDefault(),
            LastFixedAt = lastFixedAt,
            UnresolvedFailures = unresolvedFailures,
            PreviouslyFixedFailures = previouslyFixedFailures
        };
    }

    private async Task CaptureLogsAsync(ServiceInfoModel service, string path, CancellationToken cancellationToken)
    {
        var lines = new List<string>();

        // In-memory process/build logs captured by the manager.
        lines.Add("=== Manager-captured output (stdout/build) ===");
        lines.AddRange(logger.Entries
            .Where(entry => entry.Source.Equals(service.Name, StringComparison.OrdinalIgnoreCase)
                         || entry.Source.Equals("BuildRepair", StringComparison.OrdinalIgnoreCase))
            .TakeLast(500)
            .Select(entry => entry.DisplayText));

        // Structured CLEF entries written by the service itself (richer + EventCode).
        try
        {
            var fileEntries = await logFileService.ReadEntriesAsync(service.Name, 500, cancellationToken);
            if (fileEntries.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("=== Structured service log file entries (CLEF) ===");
                lines.AddRange(fileEntries.Select(entry => entry.DisplayText));
            }
        }
        catch (Exception ex)
        {
            lines.Add($"(Could not read structured log files: {ex.Message})");
        }

        await File.WriteAllLinesAsync(path, lines, cancellationToken);
    }

    private static FailureCategory MapEventCode(string? eventCode) => eventCode switch
    {
        "MIGRATION_FAILED" => FailureCategory.EfMigrationIssue,
        "DB_CONNECTION_FAILED" => FailureCategory.SqlConnectivityIssue,
        "DB_COMMAND_FAILED" => FailureCategory.SqlConnectivityIssue,
        "DI_RESOLUTION_FAILED" => FailureCategory.DependencyInjectionFailure,
        "CONFIG_MISSING" => FailureCategory.MissingAppSettings,
        "HEALTH_CHECK_FAILED" => FailureCategory.HealthCheckFailure,
        "STARTUP_FAILED" => FailureCategory.RuntimeException,
        "UNHANDLED_EXCEPTION" => FailureCategory.RuntimeException,
        "EXTERNAL_DEP_FAILED" => FailureCategory.RuntimeException,
        "JOB_FAILED" => FailureCategory.RuntimeException,
        "AUTH_FAILED" => FailureCategory.RuntimeException,
        _ => FailureCategory.Unknown
    };

    private static async Task CaptureEnvironmentAsync(ServiceInfoModel service, string path, CancellationToken cancellationToken)
    {
        var payload = new
        {
            service.Name,
            service.ProjectPath,
            service.WorkingDirectory,
            service.EnvironmentName,
            service.LaunchProfileName,
            service.HttpsUrl,
            service.HttpUrl,
            service.LastAction,
            launchSettingsPath = Path.Combine(service.WorkingDirectory, "Properties", "launchSettings.json"),
            appSettingsPath = Path.Combine(service.WorkingDirectory, "appsettings.json"),
            appSettingsDevPath = Path.Combine(service.WorkingDirectory, "appsettings.Development.json")
        };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, cancellationToken: cancellationToken);
    }

    private async Task CaptureGitAsync(ServiceInfoModel service, string path, CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        try
        {
            var branch = await processService.RunCommandAsync("git", "rev-parse --abbrev-ref HEAD", service.WorkingDirectory, service.Name, LogLevel.Process, cancellationToken);
            var status = await processService.RunCommandAsync("git", "status --short", service.WorkingDirectory, service.Name, LogLevel.Process, cancellationToken);
            var diff = await processService.RunCommandAsync("git", "diff", service.WorkingDirectory, service.Name, LogLevel.Process, cancellationToken);

            output.AppendLine($"branch_exit={branch.ExitCode}");
            output.AppendLine($"status_exit={status.ExitCode}");
            output.AppendLine($"diff_exit={diff.ExitCode}");
            output.AppendLine("Use repository git status and diff for latest details.");
        }
        catch (Exception ex)
        {
            output.AppendLine($"Git diagnostics unavailable: {ex.Message}");
        }

        await File.WriteAllTextAsync(path, output.ToString(), cancellationToken);
    }

    private static async Task CaptureHealthAsync(ServiceInfoModel service, string path, CancellationToken cancellationToken)
    {
        var payload = new
        {
            service.Name,
            service.Status,
            service.Health,
            service.LastAction,
            service.ProcessId,
            Timestamp = DateTimeOffset.UtcNow
        };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, cancellationToken: cancellationToken);
    }

    private static FailureCategory Classify(string logs, string lastAction)
    {
        if (logs.Contains("address already in use", StringComparison.OrdinalIgnoreCase) || lastAction.Contains("Port", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.PortConflict;
        }

        if (logs.Contains("NU", StringComparison.OrdinalIgnoreCase) && logs.Contains("restore", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.NuGetRestoreIssue;
        }

        if (logs.Contains("migration", StringComparison.OrdinalIgnoreCase) || logs.Contains("DbUpdateException", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.EfMigrationIssue;
        }

        if (logs.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.DependencyInjectionFailure;
        }

        if (logs.Contains("kestrel", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.KestrelBindingFailure;
        }

        if (logs.Contains("health check failed", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.HealthCheckFailure;
        }

        if (logs.Contains("Build failed", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.BuildFailure;
        }

        return FailureCategory.Unknown;
    }

    private static string BuildSummary(ServiceInfoModel service, FailureCategory category, string logs)
    {
        var excerpt = string.Join(Environment.NewLine, logs.Split(Environment.NewLine).TakeLast(40));
        return $"""
## Failure Summary

- Service: {service.Name}
- Category: {category}
- LastAction: {service.LastAction}
- Status: {service.Status}
- Health: {service.Health}

## Log Excerpt
```text
{excerpt}
```
""";
    }
}
