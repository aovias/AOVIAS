namespace ComplianceServiceManager.Operations;

/// <summary>One structured log line, always attributed to the stage that was running when it was
/// recorded — the basis for the per-stage expandable log view and for the exported log file.</summary>
public sealed record OperationLogEntry(DateTimeOffset Timestamp, OperationLogSeverity Severity, string StageId, string StageName, string Message);
