using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>
/// Read-only Git workspace inspection for Studio D1. Not a general Git API and not related to
/// <see cref="IGitSafetyService"/> repair checkpoints.
/// </summary>
public interface IGitWorkspaceService
{
    /// <summary>Returns whether <paramref name="path"/> looks like a Git worktree (.git file or directory).</summary>
    bool LooksLikeGitWorktree(string path);

    /// <summary>Lists worktrees for the repository that owns <paramref name="repositoryOrWorktreePath"/>.</summary>
    Task<IReadOnlyList<GitWorktreeInfo>> ListWorktreesAsync(
        string repositoryOrWorktreePath,
        CancellationToken cancellationToken = default);

    /// <summary>Reads branch/HEAD/clean/dirty/ahead-behind for a worktree path.</summary>
    Task<GitWorkspaceState> GetWorkspaceStateAsync(
        string worktreePath,
        CancellationToken cancellationToken = default);
}
