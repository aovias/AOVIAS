namespace ComplianceServiceManager.Models.LocalPullRequests;

/// <summary>Local PR lifecycle status. Studio metadata; Git remains source of truth for code.</summary>
public enum LocalPullRequestStatus
{
    Draft = 0,
    ReadyForReview = 1,
    InReview = 2,
    ChangesRequested = 3,
    Approved = 4,
    Merging = 5,
    Merged = 6,
    Closed = 7,
    Blocked = 8,
    Conflicted = 9,
    Failed = 10
}
