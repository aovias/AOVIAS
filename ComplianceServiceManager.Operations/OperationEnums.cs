namespace ComplianceServiceManager.Operations;

/// <summary>The lifecycle state of one stage within a running <see cref="OperationInstance"/>.</summary>
public enum OperationStageStatus
{
    Pending,
    Running,
    Completed,
    Warning,
    Failed,
    Cancelled,
    Skipped
}

/// <summary>The terminal (or in-flight) state of an operation as a whole.</summary>
public enum OperationStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum OperationLogSeverity
{
    Info,
    Warning,
    Error
}
