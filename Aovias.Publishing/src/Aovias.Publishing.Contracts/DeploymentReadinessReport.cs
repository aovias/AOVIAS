namespace Aovias.Publishing.Contracts;

using Aovias.Deployment.Contracts.Validation;

/// <summary>
/// Result of probing a runtime API endpoint during deployment readiness
/// validation (D9). Captures the expected vs actual response.
/// </summary>
public sealed class ApiProbeResult
{
    public required string Method { get; init; }

    public required string Endpoint { get; init; }

    public int ExpectedStatus { get; init; } = 200;

    public int StatusCode { get; init; }

    public string Body { get; init; } = string.Empty;

    public bool Succeeded => StatusCode == ExpectedStatus;
}

/// <summary>
/// Result of an individual deployment readiness validation stage.
/// </summary>
public sealed class ValidationStageResult
{
    public required string StageName { get; init; }

    public bool Passed { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Recommendations { get; init; } = [];

    public bool Deferred { get; init; }

    public string StatusText => Deferred
        ? "Deferred"
        : !Passed
            ? "Failed"
            : Warnings.Count > 0
                ? "Warning"
                : "Passed";

    public string DiagnosticsSummary => Passed
        ? (Warnings.Count > 0 ? string.Join(Environment.NewLine, Warnings) : "No issues detected.")
        : string.Join(Environment.NewLine, Errors);

    public static ValidationStageResult Pass(string stageName, TimeSpan duration, IReadOnlyList<string>? warnings = null, IReadOnlyList<string>? recommendations = null)
        => new()
        {
            StageName = stageName,
            Passed = true,
            Duration = duration,
            Warnings = warnings ?? [],
            Recommendations = recommendations ?? []
        };

    public static ValidationStageResult Fail(string stageName, TimeSpan duration, IReadOnlyList<string> errors, IReadOnlyList<string>? warnings = null)
        => new()
        {
            StageName = stageName,
            Passed = false,
            Duration = duration,
            Errors = errors,
            Warnings = warnings ?? []
        };

    public static ValidationStageResult Defer(
        string stageName,
        TimeSpan duration,
        string reason,
        IReadOnlyList<string>? recommendations = null)
        => new()
        {
            StageName = stageName,
            Passed = true,
            Deferred = true,
            Duration = duration,
            Recommendations = [reason, .. recommendations ?? []]
        };
}

/// <summary>
/// Deployment readiness report produced by the DRV engine.
/// </summary>
public sealed class DeploymentReadinessReport
{
    public bool IsReady { get; init; }

    public int OverallScore { get; init; }

    public string StatusText { get; init; } = string.Empty;

    public TimeSpan ValidationDuration { get; init; }

    public IReadOnlyList<ValidationStageResult> StageResults { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Recommendations { get; init; } = [];

    public IReadOnlyList<ApiProbeResult> ApiProbes { get; init; } = [];

    public string? LogFilePath { get; init; }

    public ContractValidationReport? ContractValidation { get; init; }

    public static DeploymentReadinessReport Ready(
        IReadOnlyList<ValidationStageResult> stages,
        TimeSpan duration,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? recommendations = null,
        string? logFilePath = null,
        IReadOnlyList<ApiProbeResult>? apiProbes = null,
        ContractValidationReport? contractValidation = null)
        => BuildReadyReport(
            stages,
            duration,
            "READY FOR DEPLOYMENT",
            warnings,
            recommendations,
            logFilePath,
            apiProbes,
            contractValidation);

    public static DeploymentReadinessReport PublishingReady(
        IReadOnlyList<ValidationStageResult> stages,
        TimeSpan duration,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? recommendations = null,
        string? logFilePath = null,
        IReadOnlyList<ApiProbeResult>? apiProbes = null,
        ContractValidationReport? contractValidation = null)
        => BuildReadyReport(
            stages,
            duration,
            "PUBLISHING READY",
            warnings,
            recommendations,
            logFilePath,
            apiProbes,
            contractValidation);

    private static DeploymentReadinessReport BuildReadyReport(
        IReadOnlyList<ValidationStageResult> stages,
        TimeSpan duration,
        string statusText,
        IReadOnlyList<string>? warnings,
        IReadOnlyList<string>? recommendations,
        string? logFilePath,
        IReadOnlyList<ApiProbeResult>? apiProbes,
        ContractValidationReport? contractValidation)
        => new()
        {
            IsReady = true,
            OverallScore = CalculateScore(stages),
            StatusText = statusText,
            ValidationDuration = duration,
            StageResults = stages,
            Warnings = warnings ?? [],
            Recommendations = recommendations ?? [],
            ApiProbes = apiProbes ?? [],
            LogFilePath = logFilePath,
            ContractValidation = contractValidation
        };

    public static DeploymentReadinessReport NotReady(
        IReadOnlyList<ValidationStageResult> stages,
        TimeSpan duration,
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? recommendations = null,
        string? logFilePath = null,
        IReadOnlyList<ApiProbeResult>? apiProbes = null,
        ContractValidationReport? contractValidation = null)
        => BuildNotReadyReport(
            stages,
            duration,
            errors,
            warnings,
            recommendations,
            logFilePath,
            apiProbes,
            contractValidation,
            "NOT DEPLOYMENT READY");

    public static DeploymentReadinessReport NotPublishingReady(
        IReadOnlyList<ValidationStageResult> stages,
        TimeSpan duration,
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? recommendations = null,
        string? logFilePath = null,
        IReadOnlyList<ApiProbeResult>? apiProbes = null,
        ContractValidationReport? contractValidation = null)
        => BuildNotReadyReport(
            stages,
            duration,
            errors,
            warnings,
            recommendations,
            logFilePath,
            apiProbes,
            contractValidation,
            "NOT PUBLISHING READY");

    private static int CalculateScore(IReadOnlyList<ValidationStageResult> stages)
    {
        if (stages.Count == 0)
        {
            return 0;
        }

        var satisfied = stages.Count(stage => stage.Passed || stage.Deferred);
        return (int)Math.Round(satisfied * 100d / stages.Count);
    }

    private static DeploymentReadinessReport BuildNotReadyReport(
        IReadOnlyList<ValidationStageResult> stages,
        TimeSpan duration,
        IReadOnlyList<string> errors,
        IReadOnlyList<string>? warnings,
        IReadOnlyList<string>? recommendations,
        string? logFilePath,
        IReadOnlyList<ApiProbeResult>? apiProbes,
        ContractValidationReport? contractValidation,
        string statusText)
    {
        var passed = stages.Count(stage => stage.Passed && !stage.Deferred);
        var score = stages.Count == 0 ? 0 : (int)Math.Round(passed * 100d / stages.Count);
        return new DeploymentReadinessReport
        {
            IsReady = false,
            OverallScore = score,
            StatusText = statusText,
            ValidationDuration = duration,
            StageResults = stages,
            Errors = errors,
            Warnings = warnings ?? [],
            Recommendations = recommendations ?? [],
            ApiProbes = apiProbes ?? [],
            LogFilePath = logFilePath,
            ContractValidation = contractValidation
        };
    }
}
