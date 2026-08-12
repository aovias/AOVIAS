namespace ComplianceServiceManager.Models.LocalPullRequests;

public sealed class LocalPullRequestCommitInfo
{
    public string Sha { get; init; } = string.Empty;

    public string ShortSha { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public DateTimeOffset? AuthoredAt { get; init; }
}

public sealed class LocalPullRequestFileChange
{
    public string Path { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int Additions { get; init; }

    public int Deletions { get; init; }
}

/// <summary>Live comparison derived from Git (never persisted as authoritative).</summary>
public sealed class LocalPullRequestComparison
{
    public string? SourceTipSha { get; init; }

    public string? TargetTipSha { get; init; }

    public string? MergeBaseSha { get; init; }

    public int Ahead { get; init; }

    public int Behind { get; init; }

    public bool SourceClean { get; init; }

    public bool TargetClean { get; init; }

    public bool SourceBranchExists { get; init; }

    public bool TargetBranchExists { get; init; }

    public bool HasConflicts { get; init; }

    public string? ConflictSummary { get; init; }

    public bool ApprovalIsStale { get; init; }

    public IReadOnlyList<LocalPullRequestCommitInfo> Commits { get; init; } = [];

    public IReadOnlyList<LocalPullRequestFileChange> ChangedFiles { get; init; } = [];

    public int LinesAdded { get; init; }

    public int LinesRemoved { get; init; }

    public string? Error { get; init; }
}
