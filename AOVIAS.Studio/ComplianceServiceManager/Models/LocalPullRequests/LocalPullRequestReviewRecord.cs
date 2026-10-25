namespace ComplianceServiceManager.Models.LocalPullRequests;

public sealed class LocalPullRequestReviewRecord
{
    public string Reviewer { get; set; } = "Human";

    public DateTimeOffset Timestamp { get; set; }

    public string? Comment { get; set; }

    /// <summary>Source tip SHA at the time of approval / request-changes.</summary>
    public string? SourceCommitSha { get; set; }

    /// <summary>Target tip SHA at the time of approval.</summary>
    public string? TargetCommitSha { get; set; }

    public bool IsApproval { get; set; }
}
