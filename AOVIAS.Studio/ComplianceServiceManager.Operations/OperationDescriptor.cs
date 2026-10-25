namespace ComplianceServiceManager.Operations;

/// <summary>The static, pre-run shape of an operation: what it's called, what it's targeting, and
/// its ordered stage list. Built by an <see cref="IOperationExecutor"/> before the engine starts it
/// — everything here is known before a single stage runs.</summary>
public sealed class OperationDescriptor
{
    /// <summary>The kind of operation, e.g. "Bootstrap", "Publish" — used to group history records and route the right presentation.</summary>
    public required string Kind { get; init; }

    /// <summary>Operator-facing name for this specific run, e.g. "Bootstrap vps-01.example.com".</summary>
    public required string DisplayName { get; init; }

    /// <summary>A stable identity for what this operation is acting on (e.g. a bootstrap target's <c>host:port:profile</c>). The engine enforces at most one running operation per target identity at a time, and history/attempt-numbering is keyed by it.</summary>
    public required string TargetIdentity { get; init; }

    public required IReadOnlyList<OperationStageDefinition> Stages { get; init; }

    /// <summary>Whether retrying this operation resumes from the point of failure (true) or always starts over (false). Purely descriptive for the UI — the engine's retry mechanism itself is the same re-invocation either way; whether it actually resumes is up to the executor/backend (for Bootstrap, <c>BootstrapStateStore</c>).</summary>
    public bool SupportsResume { get; init; }

    public bool SupportsRetry { get; init; } = true;

    public bool SupportsCancel { get; init; } = true;
}
