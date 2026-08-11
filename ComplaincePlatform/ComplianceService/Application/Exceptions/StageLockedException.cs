using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;

namespace ComplianceService.Application.Exceptions;

/// <summary>
/// Thrown when a user attempts to access an OE belonging to a maturity stage that
/// has not yet been unlocked (earned progression criteria not met).
/// HTTP mapping: 423 Locked.
/// </summary>
public sealed class StageLockedException : Exception
{
    public OeClassification Stage { get; }
    public decimal CurrentScore { get; }
    public decimal RequiredScore { get; }
    public IReadOnlyList<string> BlockingReasons { get; }

    public StageLockedException(
        OeClassification stage,
        decimal currentScore,
        decimal requiredScore,
        IReadOnlyList<string>? blockingReasons = null)
        : base(BuildMessage(stage, currentScore, requiredScore, blockingReasons))
    {
        Stage = stage;
        CurrentScore = currentScore;
        RequiredScore = requiredScore;
        BlockingReasons = blockingReasons ?? Array.Empty<string>();
    }

    private static string BuildMessage(
        OeClassification stage,
        decimal currentScore,
        decimal requiredScore,
        IReadOnlyList<string>? blockingReasons)
    {
        var baseMsg = $"Stage '{stage}' is locked. Readiness: {currentScore:F1}%, unlock progress: {requiredScore:F1}%.";
        if (blockingReasons is { Count: > 0 })
        {
            return baseMsg + " " + string.Join("; ", blockingReasons);
        }

        return baseMsg;
    }
}
