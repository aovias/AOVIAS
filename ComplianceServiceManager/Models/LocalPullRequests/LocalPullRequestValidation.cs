namespace ComplianceServiceManager.Models.LocalPullRequests;

public enum LocalPullRequestValidationResult
{
    Unknown = 0,
    Passed = 1,
    Failed = 2
}

public sealed class LocalPullRequestValidation
{
    public LocalPullRequestValidationResult Build { get; set; } = LocalPullRequestValidationResult.Unknown;

    public LocalPullRequestValidationResult Tests { get; set; } = LocalPullRequestValidationResult.Unknown;

    public LocalPullRequestValidationResult Security { get; set; } = LocalPullRequestValidationResult.Unknown;

    public LocalPullRequestValidationResult CompletionCheck { get; set; } = LocalPullRequestValidationResult.Unknown;

    public LocalPullRequestValidationResult Overall { get; set; } = LocalPullRequestValidationResult.Unknown;

    public DateTimeOffset? Timestamp { get; set; }

    public string? Details { get; set; }
}
