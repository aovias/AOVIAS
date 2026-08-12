namespace ComplianceServiceManager.Models.RuntimeProfiles;

public sealed class ProfileValidationIssue
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}

public sealed class ProfileValidationResult
{
    public bool IsValid => Issues.Count == 0;

    public IReadOnlyList<ProfileValidationIssue> Issues { get; init; } = [];

    public IReadOnlyList<string> StartupOrder { get; init; } = [];

    public static ProfileValidationResult Valid(IReadOnlyList<string> startupOrder)
        => new() { Issues = [], StartupOrder = startupOrder };

    public static ProfileValidationResult Invalid(params ProfileValidationIssue[] issues)
        => new() { Issues = issues, StartupOrder = [] };

    public static ProfileValidationResult Invalid(IEnumerable<ProfileValidationIssue> issues)
        => new() { Issues = issues.ToList(), StartupOrder = [] };
}
