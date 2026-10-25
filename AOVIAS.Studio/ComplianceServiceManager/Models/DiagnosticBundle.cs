namespace ComplianceServiceManager.Models;

public sealed class DiagnosticBundle
{
    public required string SessionId { get; init; }

    public required string RootDirectory { get; init; }

    public required string ServiceName { get; init; }

    public required string ServiceDirectory { get; init; }

    public required string PromptPath { get; set; }

    public required string SummaryPath { get; init; }

    public required string LogsPath { get; init; }

    public required string EnvironmentPath { get; init; }

    public required string GitDiffPath { get; init; }

    public required string HealthPath { get; init; }

    public required FailureCategory FailureCategory { get; init; }

    public required string FailureSummary { get; init; }

    /// <summary>Newest CLEF log file for the service (the primary file the AI fix should target).</summary>
    public string? PrimaryLogFile { get; init; }

    /// <summary>UTC time of the last successful AI fix for this service, if any.</summary>
    public DateTimeOffset? LastFixedAt { get; init; }

    /// <summary>Failures that still need fixing (not previously resolved and newer than the last fix).</summary>
    public IReadOnlyList<ServiceLogEntry> UnresolvedFailures { get; init; } = Array.Empty<ServiceLogEntry>();

    /// <summary>Failures whose fingerprints were already resolved in a previous fix session.</summary>
    public IReadOnlyList<ServiceLogEntry> PreviouslyFixedFailures { get; init; } = Array.Empty<ServiceLogEntry>();
}
