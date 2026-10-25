using ComplianceServiceManager.Models.LocalPullRequests;

namespace ComplianceServiceManager.Services.LocalPullRequests;

public interface ILocalPullRequestService
{
    IReadOnlyList<LocalPullRequestItem> GetPullRequests();

    event Action? PullRequestsChanged;

    Task RefreshAllAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(LocalPullRequestItem pullRequest, CancellationToken cancellationToken = default);

    Task<LocalPullRequestItem> CreateAsync(
        string title,
        string description,
        string sourceBranch,
        string targetBranch,
        string sourceWorktreePath,
        string targetWorktreePath,
        string authorAgent,
        CancellationToken cancellationToken = default);

    Task ApproveAsync(LocalPullRequestItem pullRequest, string reviewer, string? comment, CancellationToken cancellationToken = default);

    Task RequestChangesAsync(LocalPullRequestItem pullRequest, string reviewer, string comment, CancellationToken cancellationToken = default);

    Task CloseAsync(LocalPullRequestItem pullRequest);

    Task<string> LoadFileDiffAsync(LocalPullRequestItem pullRequest, string relativePath, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? Error)> MergeAsync(LocalPullRequestItem pullRequest, CancellationToken cancellationToken = default);

    IReadOnlyList<(string Branch, string WorktreePath, string Agent)> GetCandidateSourceBranches();
}
