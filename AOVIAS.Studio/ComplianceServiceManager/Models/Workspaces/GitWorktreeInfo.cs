namespace ComplianceServiceManager.Models.Workspaces;

/// <summary>One entry from <c>git worktree list</c> (Git is authoritative).</summary>
public sealed class GitWorktreeInfo
{
    public required string Path { get; init; }

    public string? Head { get; init; }

    public string? Branch { get; init; }

    public bool IsBare { get; init; }

    public bool IsDetached { get; init; }
}
