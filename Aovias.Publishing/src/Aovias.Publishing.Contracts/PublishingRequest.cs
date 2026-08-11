namespace Aovias.Publishing.Contracts;

/// <summary>
/// Request to publish a project into a deployment package.
/// </summary>
public sealed class PublishingRequest
{
    public required string ProjectPath { get; init; }

    public string? ServiceId { get; init; }

    public string? Version { get; init; }

    public string? OutputDirectory { get; init; }

    public string Publisher { get; init; } = Environment.UserName;

    public string Profile { get; init; } = "local";

    /// <summary>
    /// When true, package creation completes without DRV (for sequential solution-level validation).
    /// </summary>
    public bool DeferDeploymentReadinessValidation { get; init; }

    public string? DeploymentValidationMode { get; init; }

    /// <summary>
    /// Optional stage isolation / replay controls for Sprint 8D.
    /// </summary>
    public PublishingExecutionOptions? ExecutionOptions { get; init; }
}

/// <summary>
/// State retained between package creation and deferred DRV.
/// </summary>
public sealed class PendingPublishValidation
{
    public required string ServiceId { get; init; }

    public required string ProjectPath { get; init; }

    public required string Version { get; init; }

    public required string Profile { get; init; }

    public required string Publisher { get; init; }

    public required string StagingDirectory { get; init; }

    public required string WorkingDirectory { get; init; }

    public required string PackagePath { get; init; }

    public required string ServiceImageTarPath { get; init; }

    public int ContainerPort { get; init; } = 8080;

    public string ValidationMode { get; init; } = "Balanced";
}

/// <summary>
/// Result of a full publish operation.
/// </summary>
public sealed class PublishingResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? PackagePath { get; init; }

    public DeployPackageContents? PackageContents { get; init; }

    public IReadOnlyList<string> GeneratedFiles { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public DeploymentReadinessReport? ReadinessReport { get; init; }

    public QualityGateReport? QualityGates { get; init; }

    public IReadOnlyList<PublishingStageTiming> StageTimings { get; init; } = [];

    public bool IsDeploymentReady => ReadinessReport?.IsReady ?? false;

    public PendingPublishValidation? PendingValidation { get; init; }

    public PublishingResourceSnapshot? ResourceSnapshot { get; init; }

    public PublishingSession? Session { get; init; }

    public PublishingPipelineState? PipelineState { get; init; }

    public PublishingReliabilityReport? ReliabilityReport { get; init; }

    public bool IsDeploymentCertified => PipelineState?.IsDeploymentCertified == true;

    public static PublishingResult Success(
        string packagePath,
        DeployPackageContents? contents,
        IReadOnlyList<string> files,
        DeploymentReadinessReport? readinessReport = null,
        IReadOnlyList<string>? warnings = null,
        QualityGateReport? qualityGates = null,
        IReadOnlyList<PublishingStageTiming>? stageTimings = null,
        PublishingSession? session = null,
        PublishingPipelineState? pipelineState = null,
        PublishingReliabilityReport? reliabilityReport = null)
        => new()
        {
            IsSuccess = true,
            Message = readinessReport?.IsReady == true || pipelineState?.IsDeploymentCertified == true
                ? "Deployment Ready."
                : "Publishing completed successfully.",
            PackagePath = packagePath,
            PackageContents = contents,
            GeneratedFiles = files,
            ReadinessReport = readinessReport,
            Warnings = warnings ?? [],
            QualityGates = qualityGates,
            StageTimings = stageTimings ?? [],
            Session = session,
            PipelineState = pipelineState,
            ReliabilityReport = reliabilityReport
        };

    public static PublishingResult Failure(
        string message,
        DeploymentReadinessReport? readinessReport = null,
        PublishingSession? session = null,
        PublishingPipelineState? pipelineState = null,
        PublishingReliabilityReport? reliabilityReport = null)
        => new()
        {
            IsSuccess = false,
            Message = message,
            ReadinessReport = readinessReport,
            Session = session,
            PipelineState = pipelineState,
            ReliabilityReport = reliabilityReport,
            StageTimings = pipelineState?.Stages
                .Select(stage => new PublishingStageTiming(stage.StageName, stage.Duration, stage.Passed))
                .ToList() ?? []
        };
}

/// <summary>
/// Validation result for deployment packages.
/// </summary>
public sealed class DeployPackageValidationResult
{
    public bool IsValid { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static DeployPackageValidationResult Valid(IReadOnlyList<string>? warnings = null)
        => new() { IsValid = true, Warnings = warnings ?? [] };

    public static DeployPackageValidationResult Invalid(params string[] errors)
        => new() { IsValid = false, Errors = errors };
}
