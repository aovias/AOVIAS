namespace ComplianceServiceManager.Models;

/// <summary>
/// Operator-facing presentation of a failure: categorized, plain-language, actionable.
/// The raw diagnostic text is preserved in <see cref="Details"/> for the expandable
/// technical panel — never as the primary message.
/// </summary>
public sealed class OperatorErrorItem
{
    public required string Title { get; init; }

    public required string Category { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string SubjectDetail { get; init; } = string.Empty;

    public required string Problem { get; init; }

    public IReadOnlyList<string> RecommendedActions { get; init; } = [];

    public string Details { get; init; } = string.Empty;
}
