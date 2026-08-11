namespace ComplianceServiceManager.Models.Workspaces;

/// <summary>Live Git state for a single worktree path. Always derived; never persisted as truth.</summary>
public sealed class GitWorkspaceState
{
    public required string Path { get; init; }

    public bool IsAccessible { get; init; }

    public string? Branch { get; init; }

    public string? Head { get; init; }

    public bool IsDetached { get; init; }

    public bool IsClean { get; init; }

    public int? Ahead { get; init; }

    public int? Behind { get; init; }

    public string? Error { get; init; }

    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}
