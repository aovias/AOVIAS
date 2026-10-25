namespace ComplianceServiceManager.Models.LocalPullRequests;

/// <summary>Persisted local PR metadata. Live Git state is always refreshed from Git.</summary>
public sealed class LocalPullRequestDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RepositoryId { get; set; } = "aovias";

    public string SourceWorktreePath { get; set; } = string.Empty;

    public string TargetWorktreePath { get; set; } = string.Empty;

    public string SourceBranch { get; set; } = string.Empty;

    public string TargetBranch { get; set; } = "main";

    public string AuthorAgent { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public LocalPullRequestStatus Status { get; set; } = LocalPullRequestStatus.Draft;

    public string? BlockReason { get; set; }

    public LocalPullRequestValidation Validation { get; set; } = new();

    public LocalPullRequestReviewRecord? LastReview { get; set; }

    public string? MergeCommitSha { get; set; }

    public DateTimeOffset? MergedAt { get; set; }
}
