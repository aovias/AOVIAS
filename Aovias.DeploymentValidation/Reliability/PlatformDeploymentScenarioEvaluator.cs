namespace Aovias.DeploymentValidation.Reliability;

/// <summary>
/// Validates deployment failure/recovery messages against the Sprint 11 test matrix.
/// </summary>
public static class PlatformDeploymentScenarioEvaluator
{
    public static ScenarioEvaluationResult Evaluate(
        DeploymentScenarioDefinition scenario,
        bool operationSucceeded,
        string message)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        message ??= string.Empty;

        return scenario.ExpectedOutcome switch
        {
            DeploymentScenarioOutcome.Pass when operationSucceeded =>
                ScenarioEvaluationResult.Pass(scenario.Id, "Scenario passed as expected."),
            DeploymentScenarioOutcome.Pass =>
                ScenarioEvaluationResult.Fail(scenario.Id, $"Expected PASS but operation failed: {message}"),
            DeploymentScenarioOutcome.FailWithClearMessage when !operationSucceeded =>
                EvaluateFailureMessage(scenario, message),
            DeploymentScenarioOutcome.FailWithClearMessage when operationSucceeded =>
                ScenarioEvaluationResult.Fail(scenario.Id, "Expected failure with clear message but operation succeeded."),
            DeploymentScenarioOutcome.AutoRecover =>
                EvaluateAutoRecover(scenario, operationSucceeded, message),
            _ => ScenarioEvaluationResult.Fail(scenario.Id, "Unknown scenario outcome.")
        };
    }

    private static ScenarioEvaluationResult EvaluateFailureMessage(
        DeploymentScenarioDefinition scenario,
        string message)
    {
        if (scenario.ExpectedMessageFragments.Count == 0)
        {
            return ScenarioEvaluationResult.Pass(scenario.Id, "Failure detected.");
        }

        var matched = scenario.ExpectedMessageFragments.Any(fragment =>
            message.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        return matched
            ? ScenarioEvaluationResult.Pass(scenario.Id, "Failure message matches expected guidance.")
            : ScenarioEvaluationResult.Fail(
                scenario.Id,
                $"Failure message did not match matrix. Expected one of [{string.Join(", ", scenario.ExpectedMessageFragments)}] but got: {message}");
    }

    private static ScenarioEvaluationResult EvaluateAutoRecover(
        DeploymentScenarioDefinition scenario,
        bool operationSucceeded,
        string message)
    {
        if (operationSucceeded)
        {
            return ScenarioEvaluationResult.Pass(scenario.Id, "Auto-recovery succeeded.");
        }

        var recoverable = message.Contains("Recoverable deployment", StringComparison.OrdinalIgnoreCase)
            || scenario.ExpectedMessageFragments.Any(fragment =>
                message.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        return recoverable
            ? ScenarioEvaluationResult.Pass(scenario.Id, "Recoverable state recorded.")
            : ScenarioEvaluationResult.Fail(scenario.Id, $"Expected auto-recovery but got: {message}");
    }
}

/// <summary>
/// Result of evaluating one deployment matrix scenario.
/// </summary>
public sealed class ScenarioEvaluationResult
{
    public required string ScenarioId { get; init; }

    public bool Passed { get; init; }

    public string Message { get; init; } = string.Empty;

    public static ScenarioEvaluationResult Pass(string scenarioId, string message)
        => new() { ScenarioId = scenarioId, Passed = true, Message = message };

    public static ScenarioEvaluationResult Fail(string scenarioId, string message)
        => new() { ScenarioId = scenarioId, Passed = false, Message = message };
}
