using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.LocalPullRequests;

/// <summary>
/// Local PR lifecycle over Git. Discovery/compare are read-only; approve/request-changes/merge are explicit writes to Studio metadata (and merge to Git).
/// </summary>
public sealed class LocalPullRequestService : ILocalPullRequestService
{
    private readonly LocalPullRequestRegistry registry;
    private readonly IGitPullRequestService git;
    private readonly IGitWorkspaceService workspaceGit;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private List<LocalPullRequestItem> items = [];

    public LocalPullRequestService(
        LocalPullRequestRegistry registry,
        IGitPullRequestService git,
        IGitWorkspaceService workspaceGit,
        DevelopmentWorkspaceRegistry workspaces,
        ILoggingService logger)
    {
        this.registry = registry;
        this.git = git;
        this.workspaceGit = workspaceGit;
        this.workspaces = workspaces;
        this.logger = logger;
        ReloadFromStore();
    }

    public event Action? PullRequestsChanged;

    public IReadOnlyList<LocalPullRequestItem> GetPullRequests()
    {
        lock (gate)
        {
            return items.ToList();
        }
    }

    public IReadOnlyList<(string Branch, string WorktreePath, string Agent)> GetCandidateSourceBranches()
    {
        var results = new List<(string Branch, string WorktreePath, string Agent)>();
        foreach (var workspace in workspaces.GetEnabledWorkspaces())
        {
            if (workspace.IsMain)
            {
                continue;
            }

            try
            {
                var state = workspaceGit.GetWorkspaceStateAsync(workspace.Path).GetAwaiter().GetResult();
                if (!string.IsNullOrWhiteSpace(state.Branch) && !state.IsDetached)
                {
                    results.Add((state.Branch!, workspace.Path, workspace.AgentOwner.ToString()));
                }
            }
            catch
            {
            }
        }

        return results;
    }

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in GetPullRequests())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshAsync(item, cancellationToken);
        }

        PullRequestsChanged?.Invoke();
    }

    public async Task RefreshAsync(LocalPullRequestItem pullRequest, CancellationToken cancellationToken = default)
    {
        if (pullRequest.Status == LocalPullRequestStatus.Merged
            || pullRequest.Status == LocalPullRequestStatus.Closed)
        {
            return;
        }

        var comparison = await BuildComparisonAsync(pullRequest, cancellationToken);
        pullRequest.ApplyComparison(comparison);

        if (!string.IsNullOrWhiteSpace(comparison.Error))
        {
            pullRequest.ApplyStatus(LocalPullRequestStatus.Failed, comparison.Error);
            Persist(pullRequest);
            return;
        }

        if (!comparison.SourceBranchExists || !comparison.TargetBranchExists)
        {
            pullRequest.ApplyStatus(LocalPullRequestStatus.Blocked, "Source or target branch is missing.");
            Persist(pullRequest);
            return;
        }

        if (!comparison.SourceClean)
        {
            pullRequest.ApplyStatus(
                LocalPullRequestStatus.Blocked,
                "Source worktree contains uncommitted changes. Commit or explicitly resolve before review.");
            Persist(pullRequest);
            return;
        }

        if (comparison.HasConflicts)
        {
            pullRequest.ApplyStatus(LocalPullRequestStatus.Conflicted, comparison.ConflictSummary);
            Persist(pullRequest);
            return;
        }

        if (comparison.Ahead <= 0)
        {
            pullRequest.ApplyStatus(LocalPullRequestStatus.Blocked, "Source has no commits not already in target.");
            Persist(pullRequest);
            return;
        }

        if (pullRequest.LastReview is { IsApproval: true })
        {
            if (comparison.ApprovalIsStale)
            {
                pullRequest.ApplyStatus(
                    LocalPullRequestStatus.ReadyForReview,
                    "Approval is stale — source or target SHA changed since approval. Re-approve after review.");
                Persist(pullRequest);
                return;
            }

            pullRequest.ApplyStatus(LocalPullRequestStatus.Approved);
            Persist(pullRequest);
            return;
        }

        if (pullRequest.LastReview is { IsApproval: false })
        {
            // New commits after request-changes → ready again (not auto-approved).
            var requestedSha = pullRequest.LastReview.SourceCommitSha;
            if (!string.IsNullOrWhiteSpace(requestedSha)
                && !string.Equals(requestedSha, comparison.SourceTipSha, StringComparison.OrdinalIgnoreCase))
            {
                pullRequest.ApplyStatus(LocalPullRequestStatus.ReadyForReview);
            }
            else
            {
                pullRequest.ApplyStatus(LocalPullRequestStatus.ChangesRequested, pullRequest.LastReview.Comment);
            }

            Persist(pullRequest);
            return;
        }

        if (pullRequest.Status is LocalPullRequestStatus.Draft or LocalPullRequestStatus.Blocked or LocalPullRequestStatus.Failed)
        {
            pullRequest.ApplyStatus(LocalPullRequestStatus.ReadyForReview);
        }

        Persist(pullRequest);
    }

    public async Task<LocalPullRequestItem> CreateAsync(
        string title,
        string description,
        string sourceBranch,
        string targetBranch,
        string sourceWorktreePath,
        string targetWorktreePath,
        string authorAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(sourceBranch) || string.IsNullOrWhiteSpace(targetBranch))
        {
            throw new InvalidOperationException("Source and target branches are required.");
        }

        if (string.Equals(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and target branches must differ.");
        }

        if (string.Equals(sourceBranch, "main", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source branch cannot be main.");
        }

        EnsureRegisteredPath(sourceWorktreePath);
        EnsureRegisteredPath(targetWorktreePath);

        if (!PathsShareRepository(sourceWorktreePath, targetWorktreePath))
        {
            throw new InvalidOperationException("Source and target must belong to the same Git repository.");
        }

        var item = new LocalPullRequestItem
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            SourceBranch = sourceBranch.Trim(),
            TargetBranch = targetBranch.Trim(),
            SourceWorktreePath = Path.GetFullPath(sourceWorktreePath),
            TargetWorktreePath = Path.GetFullPath(targetWorktreePath),
            AuthorAgent = string.IsNullOrWhiteSpace(authorAgent) ? "Unknown" : authorAgent.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Validation = new LocalPullRequestValidation
            {
                Overall = LocalPullRequestValidationResult.Unknown,
                Details = "Validation has not been executed."
            }
        };
        item.ApplyStatus(LocalPullRequestStatus.Draft);

        lock (gate)
        {
            items.Add(item);
        }

        Persist(item);
        await RefreshAsync(item, cancellationToken);
        logger.Info("LocalPR", $"Created local PR '{item.Title}' ({item.Id}): {item.SourceBranch} → {item.TargetBranch}");
        PullRequestsChanged?.Invoke();
        return item;
    }

    public async Task ApproveAsync(
        LocalPullRequestItem pullRequest,
        string reviewer,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        await RefreshAsync(pullRequest, cancellationToken);
        if (pullRequest.Status is LocalPullRequestStatus.Blocked or LocalPullRequestStatus.Conflicted or LocalPullRequestStatus.Failed)
        {
            throw new InvalidOperationException(pullRequest.BlockReason ?? "PR cannot be approved in its current state.");
        }

        if (pullRequest.Comparison is null
            || string.IsNullOrWhiteSpace(pullRequest.Comparison.SourceTipSha)
            || string.IsNullOrWhiteSpace(pullRequest.Comparison.TargetTipSha))
        {
            throw new InvalidOperationException("Unable to capture Git SHAs for approval.");
        }

        if (!pullRequest.Comparison.SourceClean)
        {
            throw new InvalidOperationException("Cannot approve while source worktree is dirty.");
        }

        pullRequest.LastReview = new LocalPullRequestReviewRecord
        {
            Reviewer = string.IsNullOrWhiteSpace(reviewer) ? "Human" : reviewer.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
            Comment = comment,
            SourceCommitSha = pullRequest.Comparison.SourceTipSha,
            TargetCommitSha = pullRequest.Comparison.TargetTipSha,
            IsApproval = true
        };
        pullRequest.ApplyStatus(LocalPullRequestStatus.Approved);
        Persist(pullRequest);
        logger.Info("LocalPR", $"Approved local PR {pullRequest.Id} @ {pullRequest.LastReview.SourceCommitSha}");
        PullRequestsChanged?.Invoke();
    }

    public async Task RequestChangesAsync(
        LocalPullRequestItem pullRequest,
        string reviewer,
        string comment,
        CancellationToken cancellationToken = default)
    {
        await RefreshAsync(pullRequest, cancellationToken);
        pullRequest.LastReview = new LocalPullRequestReviewRecord
        {
            Reviewer = string.IsNullOrWhiteSpace(reviewer) ? "Human" : reviewer.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
            Comment = comment,
            SourceCommitSha = pullRequest.Comparison?.SourceTipSha,
            TargetCommitSha = pullRequest.Comparison?.TargetTipSha,
            IsApproval = false
        };
        pullRequest.ApplyStatus(LocalPullRequestStatus.ChangesRequested, comment);
        Persist(pullRequest);
        logger.Info("LocalPR", $"Requested changes on local PR {pullRequest.Id}");
        PullRequestsChanged?.Invoke();
    }

    public Task CloseAsync(LocalPullRequestItem pullRequest)
    {
        pullRequest.ApplyStatus(LocalPullRequestStatus.Closed);
        Persist(pullRequest);
        PullRequestsChanged?.Invoke();
        return Task.CompletedTask;
    }

    public async Task<string> LoadFileDiffAsync(
        LocalPullRequestItem pullRequest,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        return await git.GetFileDiffAsync(
            pullRequest.TargetWorktreePath,
            pullRequest.SourceBranch,
            pullRequest.TargetBranch,
            relativePath,
            cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> MergeAsync(
        LocalPullRequestItem pullRequest,
        CancellationToken cancellationToken = default)
    {
        await RefreshAsync(pullRequest, cancellationToken);

        var gateError = EvaluateMergeGate(pullRequest);
        if (gateError is not null)
        {
            return (false, gateError);
        }

        pullRequest.ApplyStatus(LocalPullRequestStatus.Merging);
        Persist(pullRequest);

        var message = $"Merge local PR '{pullRequest.Title}' ({pullRequest.SourceBranch})";
        var (succeeded, mergeSha, error) = await git.MergeNoFfAsync(
            pullRequest.TargetWorktreePath,
            pullRequest.SourceBranch,
            message,
            cancellationToken);

        if (!succeeded)
        {
            pullRequest.ApplyStatus(LocalPullRequestStatus.Conflicted, error);
            Persist(pullRequest);
            PullRequestsChanged?.Invoke();
            return (false, error ?? "Merge failed.");
        }

        pullRequest.MergeCommitSha = mergeSha;
        pullRequest.UpdatedAt = DateTimeOffset.UtcNow;
        pullRequest.ApplyStatus(LocalPullRequestStatus.Merged);
        Persist(pullRequest);
        logger.Info("LocalPR", $"Merged local PR {pullRequest.Id} as {mergeSha}");
        PullRequestsChanged?.Invoke();
        return (true, null);
    }

    private string? EvaluateMergeGate(LocalPullRequestItem pullRequest)
    {
        if (pullRequest.Status != LocalPullRequestStatus.Approved)
        {
            return $"PR must be APPROVED before merge (current: {pullRequest.StatusDisplay}).";
        }

        if (pullRequest.LastReview is not { IsApproval: true })
        {
            return "Explicit approval is required before merge.";
        }

        var comparison = pullRequest.Comparison;
        if (comparison is null)
        {
            return "Git comparison is unavailable.";
        }

        if (!comparison.SourceClean)
        {
            return "Source worktree is dirty.";
        }

        if (!comparison.TargetClean)
        {
            return "Target worktree is dirty.";
        }

        if (!string.Equals(comparison.SourceTipSha, pullRequest.LastReview.SourceCommitSha, StringComparison.OrdinalIgnoreCase))
        {
            return "Source tip no longer matches the approved source SHA.";
        }

        if (!string.Equals(comparison.TargetTipSha, pullRequest.LastReview.TargetCommitSha, StringComparison.OrdinalIgnoreCase))
        {
            return "Target tip no longer matches the approved target SHA.";
        }

        if (comparison.HasConflicts)
        {
            return "Merge conflicts detected. Resolve manually — Studio will not auto-resolve.";
        }

        if (string.Equals(pullRequest.SourceBranch, "main", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot merge from main.";
        }

        if (!PathsShareRepository(pullRequest.SourceWorktreePath, pullRequest.TargetWorktreePath))
        {
            return "Source and target are not in the same repository.";
        }

        if (pullRequest.Validation.Overall == LocalPullRequestValidationResult.Failed)
        {
            return "Validation failed — merge blocked.";
        }

        return null;
    }

    private async Task<LocalPullRequestComparison> BuildComparisonAsync(
        LocalPullRequestItem pullRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var sourceExists = await git.BranchExistsAsync(pullRequest.SourceWorktreePath, pullRequest.SourceBranch, cancellationToken);
            var targetExists = await git.BranchExistsAsync(pullRequest.TargetWorktreePath, pullRequest.TargetBranch, cancellationToken);
            var sourceClean = await git.IsWorkingTreeCleanAsync(pullRequest.SourceWorktreePath, cancellationToken);
            var targetClean = await git.IsWorkingTreeCleanAsync(pullRequest.TargetWorktreePath, cancellationToken);
            var sourceTip = sourceExists
                ? await git.GetTipShaAsync(pullRequest.SourceWorktreePath, pullRequest.SourceBranch, cancellationToken)
                : null;
            var targetTip = targetExists
                ? await git.GetTipShaAsync(pullRequest.TargetWorktreePath, pullRequest.TargetBranch, cancellationToken)
                : null;

            var mergeBase = sourceExists && targetExists
                ? await git.GetMergeBaseAsync(pullRequest.TargetWorktreePath, pullRequest.SourceBranch, pullRequest.TargetBranch, cancellationToken)
                : null;

            var (ahead, behind) = sourceExists && targetExists
                ? await git.GetAheadBehindAsync(pullRequest.TargetWorktreePath, pullRequest.SourceBranch, pullRequest.TargetBranch, cancellationToken)
                : (0, 0);

            var commits = sourceExists && targetExists
                ? await git.GetCommitsAsync(pullRequest.TargetWorktreePath, pullRequest.SourceBranch, pullRequest.TargetBranch, cancellationToken)
                : [];

            var files = sourceExists && targetExists
                ? await git.GetChangedFilesAsync(pullRequest.TargetWorktreePath, pullRequest.SourceBranch, pullRequest.TargetBranch, cancellationToken)
                : [];

            var (additions, deletions) = (files.Sum(f => f.Additions), files.Sum(f => f.Deletions));

            var (hasConflicts, conflictSummary) = sourceExists && targetExists
                ? await git.DetectMergeConflictsAsync(pullRequest.TargetWorktreePath, pullRequest.SourceBranch, pullRequest.TargetBranch, cancellationToken)
                : (false, null);

            var approvalStale = false;
            if (pullRequest.LastReview is { IsApproval: true } review)
            {
                approvalStale =
                    !string.Equals(review.SourceCommitSha, sourceTip, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(review.TargetCommitSha, targetTip, StringComparison.OrdinalIgnoreCase);
            }

            return new LocalPullRequestComparison
            {
                SourceTipSha = sourceTip,
                TargetTipSha = targetTip,
                MergeBaseSha = mergeBase,
                Ahead = ahead,
                Behind = behind,
                SourceClean = sourceClean,
                TargetClean = targetClean,
                SourceBranchExists = sourceExists,
                TargetBranchExists = targetExists,
                HasConflicts = hasConflicts,
                ConflictSummary = conflictSummary,
                ApprovalIsStale = approvalStale,
                Commits = commits,
                ChangedFiles = files,
                LinesAdded = additions,
                LinesRemoved = deletions
            };
        }
        catch (Exception ex)
        {
            return new LocalPullRequestComparison
            {
                Error = ex.Message
            };
        }
    }

    private void ReloadFromStore()
    {
        lock (gate)
        {
            items = registry.GetAll().Select(LocalPullRequestItem.FromDefinition).ToList();
        }
    }

    private void Persist(LocalPullRequestItem item)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;
        registry.Upsert(item.ToDefinition());
    }

    private void EnsureRegisteredPath(string path)
    {
        var full = Path.GetFullPath(path);
        var allowed = workspaces.GetAllowedRoots();
        var ok = allowed.Any(root =>
            string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(root.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        if (!ok)
        {
            throw new UnauthorizedAccessException("Path is outside registered AOVIAS development workspaces.");
        }
    }

    private static bool PathsShareRepository(string pathA, string pathB)
    {
        try
        {
            var a = Path.GetFullPath(pathA).TrimEnd('\\', '/');
            var b = Path.GetFullPath(pathB).TrimEnd('\\', '/');
            // Sibling worktrees under DevelopmentRepo share one .git (AOVIAS / AOVIAS.Cursor / AOVIAS.Claude).
            var parentA = Path.GetDirectoryName(a);
            var parentB = Path.GetDirectoryName(b);
            return string.Equals(parentA, parentB, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
