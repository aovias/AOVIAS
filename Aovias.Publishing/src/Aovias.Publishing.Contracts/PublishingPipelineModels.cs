namespace Aovias.Publishing.Contracts;

/// <summary>
/// Canonical publishing pipeline stages for Sprint 8D.
/// </summary>
public enum PublishingPipelineStageId
{
    Discovery,
    Preflight,
    Restore,
    Build,
    DockerBuild,
    ContractGeneration,
    ConfigurationGeneration,
    ComposeGeneration,
    PackageGeneration,
    PackageVerification,
    DeploymentReadinessValidation,
    ReleaseLibrary,
    DeploymentCertification
}

/// <summary>
/// Lifecycle state reported by each pipeline stage.
/// </summary>
public enum PublishingStageState
{
    NotStarted,
    Running,
    Passed,
    Skipped,
    Warning,
    Failed,
    Cancelled
}

/// <summary>
/// Deterministic failure classification for publishing diagnostics.
/// </summary>
public enum PublishingFailureCategory
{
    None,
    Build,
    Docker,
    Contracts,
    Configuration,
    Compose,
    Package,
    Verification,
    Drv,
    Runtime,
    Infrastructure,
    ExternalDependency
}

/// <summary>
/// Structured result for a single pipeline stage execution.
/// </summary>
public sealed record PublishingStageExecutionResult
{
    public required PublishingPipelineStageId StageId { get; init; }

    public required string StageName { get; init; }

    public PublishingStageState State { get; init; } = PublishingStageState.NotStarted;

    public DateTimeOffset? StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyList<string> Inputs { get; init; } = [];

    public IReadOnlyList<string> Outputs { get; init; } = [];

    public IReadOnlyList<string> Artifacts { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public string? ExceptionType { get; init; }

    public string? ExceptionMessage { get; init; }

    public string? Recommendation { get; init; }

    public string? RecoveryAction { get; init; }

    public PublishingFailureCategory FailureCategory { get; init; } = PublishingFailureCategory.None;

    public int RetryCount { get; init; }

    public bool ReusedArtifacts { get; init; }

    public bool Passed => State is PublishingStageState.Passed or PublishingStageState.Warning;

    public static PublishingStageExecutionResult CreateRunning(PublishingPipelineStageId stageId, string stageName)
        => new()
        {
            StageId = stageId,
            StageName = stageName,
            State = PublishingStageState.Running,
            StartedUtc = DateTimeOffset.UtcNow
        };

    public PublishingStageExecutionResult Complete(PublishingStageState state, TimeSpan duration, string? message = null)
        => this with
        {
            State = state,
            CompletedUtc = DateTimeOffset.UtcNow,
            Duration = duration,
            Errors = state == PublishingStageState.Failed && !string.IsNullOrWhiteSpace(message)
                ? [message!]
                : Errors
        };
}

/// <summary>
/// Aggregate pipeline state for a publish run.
/// </summary>
public sealed class PublishingPipelineState
{
    public required string SessionId { get; init; }

    public IReadOnlyList<PublishingStageExecutionResult> Stages { get; init; } = [];

    public bool IsComplete { get; init; }

    public bool IsSuccessful { get; init; }

    public bool IsDeploymentCertified { get; init; }

    public PublishingFailureCategory? TerminalFailureCategory { get; init; }

    public string? TerminalFailureStage { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public PublishingStageExecutionResult? GetStage(PublishingPipelineStageId stageId)
        => Stages.FirstOrDefault(stage => stage.StageId == stageId);
}

/// <summary>
/// Reproducible metadata captured for every publish session.
/// </summary>
public sealed record PublishingSession
{
    public required string SessionId { get; init; }

    public required string MachineName { get; init; }

    public required string OperatingSystem { get; init; }

    public string? DockerVersion { get; init; }

    public string? DotNetSdkVersion { get; init; }

    public string? AngularVersion { get; init; }

    public string? GitCommit { get; init; }

    public required string Profile { get; init; }

    public string? SolutionVersion { get; init; }

    public required string ProjectPath { get; init; }

    public string? ServiceId { get; init; }

    public string? PackageVersion { get; init; }

    public required string WorkspaceRoot { get; init; }

    public required string LogsRoot { get; init; }

    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedUtc { get; init; }

    public PublishingPipelineState? PipelineState { get; init; }

    public IReadOnlyList<string> ArtifactPaths { get; init; } = [];
}

/// <summary>
/// Controls isolated or replay execution of pipeline stages.
/// </summary>
public sealed class PublishingExecutionOptions
{
    public PublishingPipelineStageId? StartFromStage { get; init; }

    public PublishingPipelineStageId? StopAfterStage { get; init; }

    public IReadOnlyList<PublishingPipelineStageId> OnlyStages { get; init; } = [];

    public bool SkipReleaseLibrary { get; init; }

    public bool SkipDeploymentCertification { get; init; }
}

/// <summary>
/// Golden package comparison result.
/// </summary>
public sealed class GoldenPackageComparisonResult
{
    public bool Matches { get; init; }

    public bool RequiresApproval { get; init; }

    public IReadOnlyList<string> Differences { get; init; } = [];

    public string? GoldenPackagePath { get; init; }

    public string? CandidatePackagePath { get; init; }
}

/// <summary>
/// Publishing reliability metrics for Sprint 8D reporting.
/// </summary>
public sealed class PublishingReliabilityReport
{
    public required string SessionId { get; init; }

    public bool Success { get; init; }

    public double SuccessRate { get; init; }

    public TimeSpan AverageStageDuration { get; init; }

    public int StageFailureCount { get; init; }

    public int RetryCount { get; init; }

    public int RecoveryEventCount { get; init; }

    public bool PackageIntegrityVerified { get; init; }

    public bool DeterministicFingerprintMatched { get; init; }

    public bool RegressionStatusPassed { get; init; }

    public GoldenPackageComparisonResult? GoldenComparison { get; init; }

    public IReadOnlyList<PublishingStageExecutionResult> StageResults { get; init; } = [];
}

/// <summary>
/// Extended package verification including deterministic fingerprint checks.
/// </summary>
public sealed class PackageVerificationReport
{
    public bool IsValid { get; init; }

    public bool IsDeploymentCertified { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> VerifiedFiles { get; init; } = [];

    public int ChecksumCount { get; init; }

    public bool HasDeploymentCertificate { get; init; }

    public string? DeterministicFingerprint { get; init; }

    public static PackageVerificationReport Failure(IReadOnlyList<string> errors)
        => new() { IsValid = false, Errors = errors };

    public static PackageVerificationReport Success(
        IReadOnlyList<string> verifiedFiles,
        int checksumCount,
        bool hasCertificate,
        string? fingerprint,
        bool certified = false)
        => new()
        {
            IsValid = true,
            VerifiedFiles = verifiedFiles,
            ChecksumCount = checksumCount,
            HasDeploymentCertificate = hasCertificate,
            DeterministicFingerprint = fingerprint,
            IsDeploymentCertified = certified
        };
}
