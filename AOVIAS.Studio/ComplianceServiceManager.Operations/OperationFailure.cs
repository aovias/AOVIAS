namespace ComplianceServiceManager.Operations;

/// <summary>Categorized failure information for a stage or an operation as a whole — the shape
/// every operation-engine consumer's primary UI renders instead of a raw exception message. Mirrors
/// the same "no raw stack traces in primary UI" pattern already established for Publishing
/// (<c>OperatorErrorItem</c>) and the original Bootstrap wizard (<c>BootstrapFailureInfo</c>); this
/// type supersedes both for anything running through the Operation Engine.</summary>
public sealed class OperationFailure
{
    /// <summary>What went wrong, in one sentence — e.g. "Docker installation failed."</summary>
    public required string Problem { get; init; }

    /// <summary>Why it happened, in operator language — not a raw exception message.</summary>
    public required string Cause { get; init; }

    /// <summary>What the operator should do next.</summary>
    public required string RecommendedAction { get; init; }

    /// <summary>The full raw diagnostic (exception text, command stderr, etc.) — only ever shown behind an explicit "Advanced Details" expander, never inline.</summary>
    public string Details { get; init; } = string.Empty;
}
