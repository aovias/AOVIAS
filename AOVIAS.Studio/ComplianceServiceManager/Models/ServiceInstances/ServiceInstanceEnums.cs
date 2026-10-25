namespace ComplianceServiceManager.Models.ServiceInstances;

/// <summary>
/// Where a service instance executes conceptually.
/// Shared = shared runtime; Workspace = workspace-scoped (Cursor/Claude/etc. via WorkspaceId).
/// </summary>
public enum ServiceExecutionScope
{
    Shared = 0,
    Workspace = 1
}

/// <summary>Who is responsible for the instance metadata (not OS process ownership).</summary>
public enum ServiceInstanceOwnership
{
    Shared = 0,
    Workspace = 1
}

/// <summary>
/// Reconciled instance lifecycle display state.
/// Runtime truth comes from D2/S3 — this is not a second process state machine.
/// </summary>
public enum ServiceInstanceState
{
    Defined = 0,
    Starting = 1,
    Running = 2,
    Stopping = 3,
    Stopped = 4,
    Failed = 5,
    Unavailable = 6,
    Unknown = 7
}
