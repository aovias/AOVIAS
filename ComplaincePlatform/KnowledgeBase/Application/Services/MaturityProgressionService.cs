namespace KnowledgeBase.Application.Services;

/// <summary>Result returned by the progression evaluation.</summary>
public sealed class MaturityProgressionResult
{
    public bool    CanProgress    { get; init; }
    public string? NextLevel      { get; init; }
    public string? BlockingReason { get; init; }
}

/// <summary>
/// Evaluates whether a user can progress from the current maturity level to the next,
/// based on their score and critical failure count.
///
/// Rules:
///   Core       → Commitment  : score ≥ 70 AND criticalFailures == 0
///   Commitment → Achievement : score ≥ 75
///   Achievement→ Excellence  : score ≥ 85
/// </summary>
public sealed class MaturityProgressionService
{
    private static readonly Dictionary<string, (string Next, decimal RequiredScore, bool RequireZeroCritical)>
        Rules = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Core"]        = ("Commitment",  70m, true),
            ["Commitment"]  = ("Achievement", 75m, false),
            ["Achievement"] = ("Excellence",  85m, false)
        };

    public MaturityProgressionResult EvaluateProgression(
        string  currentLevel,
        decimal score,
        int     criticalFailures)
    {
        if (!Rules.TryGetValue(currentLevel, out var rule))
        {
            return new MaturityProgressionResult
            {
                CanProgress    = false,
                BlockingReason = $"'{currentLevel}' is the highest level or unknown."
            };
        }

        if (rule.RequireZeroCritical && criticalFailures > 0)
        {
            return new MaturityProgressionResult
            {
                CanProgress    = false,
                NextLevel      = rule.Next,
                BlockingReason = $"{criticalFailures} critical failure(s) must be resolved before progressing."
            };
        }

        if (score < rule.RequiredScore)
        {
            return new MaturityProgressionResult
            {
                CanProgress    = false,
                NextLevel      = rule.Next,
                BlockingReason = $"Score {score:F1}% is below the {rule.RequiredScore}% threshold required to unlock {rule.Next}."
            };
        }

        return new MaturityProgressionResult
        {
            CanProgress = true,
            NextLevel   = rule.Next
        };
    }
}
