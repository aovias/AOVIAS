using ComplianceServiceManager.Models.LocalPullRequests;

namespace ComplianceServiceManager.Services.LocalPullRequests;

/// <summary>
/// Git capabilities required by local PRs. Constrained to allowlisted worktrees via <see cref="Workspaces.GitCliRunner"/>.
/// Distinct from <c>IGitSafetyService</c> repair checkpoints.
/// </summary>
public interface IGitPullRequestService
{
    Task<bool> BranchExistsAsync(string worktreePath, string branch, CancellationToken cancellationToken = default);

    Task<string?> GetTipShaAsync(string worktreePath, string branchOrRef, CancellationToken cancellationToken = default);

    Task<string?> GetMergeBaseAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    Task<(int Ahead, int Behind)> GetAheadBehindAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    Task<bool> IsWorkingTreeCleanAsync(string worktreePath, CancellationToken cancellationToken = default);

    /// <summary>Current checked-out branch name for the worktree (<c>HEAD</c>), or null if detached/unavailable.</summary>
    Task<string?> GetCurrentBranchAsync(string worktreePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalPullRequestCommitInfo>> GetCommitsAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalPullRequestFileChange>> GetChangedFilesAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    Task<(int Additions, int Deletions)> GetDiffStatAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    Task<string> GetFileDiffAsync(string worktreePath, string sourceBranch, string targetBranch, string relativePath, CancellationToken cancellationToken = default);

    Task<(bool HasConflicts, string? Summary)> DetectMergeConflictsAsync(string worktreePath, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicit --no-ff merge of <paramref name="sourceBranch"/> into <paramref name="expectedTargetBranch"/>
    /// checked out in <paramref name="targetWorktreePath"/>. Fails closed if HEAD is not that branch.
    /// Caller must have already verified safety gates. Does not reset/clean/rebase/checkout.
    /// </summary>
    Task<(bool Succeeded, string? MergeCommitSha, string? Error)> MergeNoFfAsync(
        string targetWorktreePath,
        string sourceBranch,
        string expectedTargetBranch,
        string message,
        CancellationToken cancellationToken = default);
}
