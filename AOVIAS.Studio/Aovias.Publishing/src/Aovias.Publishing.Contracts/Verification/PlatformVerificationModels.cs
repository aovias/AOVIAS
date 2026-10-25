namespace Aovias.Publishing.Contracts.Verification;

/// <summary>
/// Sprint M3.3 — End-to-End Verification Pipeline.
///
/// Evidence tier for a single verification claim, mirroring
/// <c>IMPLEMENTATION_GUIDELINES.md</c> §11 (Execution Honesty Rule). Every
/// <see cref="VerificationStageStatus"/> carries one of these so a report reader can tell,
/// per claim, how much confidence it deserves — this pipeline never reports "Passed" for a
/// claim it did not actually execute.
/// </summary>
public enum VerificationEvidenceTier
{
    /// <summary>Code was read/reasoned about; no tool executed. Never used for a Passed outcome.</summary>
    SourceAnalysis,

    /// <summary>A deterministic, non-SDK check actually ran (path/reference resolution, model cross-checks, etc.).</summary>
    StaticVerification,

    /// <summary>A real tool executed (dotnet CLI, Docker CLI, the publishing pipeline itself).</summary>
    RuntimeVerification,

    /// <summary>Full path exercised against a real or disposable target environment.</summary>
    EndToEnd
}

/// <summary>
/// Outcome of a single verification stage.
/// </summary>
public enum VerificationOutcome
{
    Passed,
    Failed,
    Skipped,
    Blocked
}

/// <summary>
/// Result of one verification stage (Build, Test, Publish, Package, Fingerprint, Contract,
/// Manifest, Deployment Plan). Carries a root cause on failure, per the sprint's Reporting
/// requirement ("every failure must include a clear root cause").
/// </summary>
public sealed class VerificationStageStatus
{
    public required VerificationOutcome Outcome { get; init; }

    public required VerificationEvidenceTier EvidenceTier { get; init; }

    public required string Message { get; init; }

    /// <summary>Populated only when <see cref="Outcome"/> is <see cref="VerificationOutcome.Failed"/> or <see cref="VerificationOutcome.Blocked"/>.</summary>
    public string? RootCause { get; init; }

    public IReadOnlyList<string> Details { get; init; } = [];

    public bool IsPassed => Outcome == VerificationOutcome.Passed;

    public static VerificationStageStatus Passed(string message, VerificationEvidenceTier tier, IReadOnlyList<string>? details = null)
        => new() { Outcome = VerificationOutcome.Passed, EvidenceTier = tier, Message = message, Details = details ?? [] };

    public static VerificationStageStatus Failed(string message, string rootCause, VerificationEvidenceTier tier, IReadOnlyList<string>? details = null)
        => new() { Outcome = VerificationOutcome.Failed, EvidenceTier = tier, Message = message, RootCause = rootCause, Details = details ?? [] };

    public static VerificationStageStatus Skipped(string reason, VerificationEvidenceTier tier = VerificationEvidenceTier.SourceAnalysis)
        => new() { Outcome = VerificationOutcome.Skipped, EvidenceTier = tier, Message = reason };

    public static VerificationStageStatus Blocked(string reason, VerificationEvidenceTier tier = VerificationEvidenceTier.RuntimeVerification)
        => new() { Outcome = VerificationOutcome.Blocked, EvidenceTier = tier, Message = reason, RootCause = reason };
}

/// <summary>
/// Per-project breakdown within a platform-wide verification run.
/// </summary>
public sealed class ProjectVerificationReport
{
    public required string ServiceId { get; init; }

    public required VerificationStageStatus PackageValidation { get; init; }

    public required VerificationStageStatus FingerprintValidation { get; init; }

    public required VerificationStageStatus ContractValidation { get; init; }

    public required VerificationStageStatus ManifestValidation { get; init; }

    public required VerificationStageStatus DeploymentPlanValidation { get; init; }

    public bool OverallResult =>
        PackageValidation.Outcome is VerificationOutcome.Passed or VerificationOutcome.Skipped
        && FingerprintValidation.Outcome is VerificationOutcome.Passed or VerificationOutcome.Skipped
        && ContractValidation.Outcome is VerificationOutcome.Passed or VerificationOutcome.Skipped
        && ManifestValidation.Outcome is VerificationOutcome.Passed or VerificationOutcome.Skipped
        && DeploymentPlanValidation.Outcome is VerificationOutcome.Passed or VerificationOutcome.Skipped;
}

/// <summary>
/// Request to run the full Build -> Test -> Publish -> Package -> Verification pipeline.
/// </summary>
public sealed class PlatformVerificationRequest
{
    public string? PlatformRoot { get; init; }

    /// <summary>Path to the consolidated CI solution. Defaults to <c>Aovias.Platform.slnx</c> at the platform root (Sprint M3.2).</summary>
    public string? SolutionPath { get; init; }

    public string Profile { get; init; } = "demo";

    public string Version { get; init; } = "1.0.0";

    public string? OutputDirectory { get; init; }

    public string Publisher { get; init; } = Environment.UserName;

    /// <summary>
    /// When true (default), runs solution-level <c>dotnet build</c>/<c>dotnet test</c> before
    /// publishing. Requires a real .NET SDK; if unavailable, Build/Test stages report
    /// <see cref="VerificationOutcome.Blocked"/> rather than a fabricated result.
    /// </summary>
    public bool VerifySolutionBuildAndTest { get; init; } = true;
}

/// <summary>
/// Single, unified verification result for one end-to-end run: Build -> Test -> Publish ->
/// Package -> Fingerprint -> Contract -> Manifest -> Deployment Plan (Sprint M3.3), extended in
/// Sprint M3.4 with the deploy-time lifecycle: Runtime Startup -> Health Gates -> Deployment
/// Certification -> Rollback Readiness -> Reconciliation Initialization. This is deliberately
/// the same report growing across sprints, not a parallel "Deployment Certification" report type
/// — see the M3.4 report's "no duplicated verification logic" note.
/// </summary>
public sealed record PlatformVerificationReport
{
    public required VerificationStageStatus BuildStatus { get; init; }

    public required VerificationStageStatus TestStatus { get; init; }

    public required VerificationStageStatus PublishStatus { get; init; }

    public required VerificationStageStatus PackageValidation { get; init; }

    public required VerificationStageStatus FingerprintValidation { get; init; }

    public required VerificationStageStatus ContractValidation { get; init; }

    public required VerificationStageStatus ManifestValidation { get; init; }

    /// <summary>
    /// Additive to the sprint's named Reporting fields: ROADMAP scope explicitly calls for
    /// "Verify Deployment Plan generation," which has no home among the other seven fields.
    /// </summary>
    public required VerificationStageStatus DeploymentPlanValidation { get; init; }

    /// <summary>
    /// Sprint M3.4: does the Runtime Agent's own startup-diagnostics wiring exist (bootstrap
    /// report, configuration/profile/logging/storage checks, <c>StartAsync</c> call)? Structural
    /// only — whether the host actually starts and stays up requires a live SDK/runtime.
    /// </summary>
    public required VerificationStageStatus RuntimeStartupValidation { get; init; }

    /// <summary>
    /// Sprint M3.4: does a representative <c>DeploymentPlan</c> declare at least one
    /// <c>HealthGate</c>, and is <c>HealthGateTaskProvisioner</c> wired into the execution
    /// provisioner registry? Whether a live health probe actually passes requires a deployed
    /// target.
    /// </summary>
    public required VerificationStageStatus HealthGateValidation { get; init; }

    /// <summary>
    /// Sprint M3.4: deploy-time acceptance gates (<c>CertificationGate</c> /
    /// <c>CertificationTaskProvisioner</c>) — distinct from Sprint M3.3's publish-time
    /// <c>DeploymentCertification</c> package stage. Structural readiness only.
    /// </summary>
    public required VerificationStageStatus DeploymentCertificationValidation { get; init; }

    /// <summary>Sprint M3.4: does a representative <c>DeploymentPlan</c> declare rollback checkpoints and a recovery strategy?</summary>
    public required VerificationStageStatus RollbackReadinessValidation { get; init; }

    /// <summary>Sprint M3.4: is <c>ReconciliationSchedulerHostedService</c> registered as a hosted service in Runtime Agent's DI wiring? Whether it actually starts and ticks requires a live host.</summary>
    public required VerificationStageStatus ReconciliationInitializationValidation { get; init; }

    public IReadOnlyList<ProjectVerificationReport> ProjectReports { get; init; } = [];

    public required bool OverallResult { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    public required TimeSpan TotalDuration { get; init; }

    public string? ReportPath { get; init; }
}

/// <summary>
/// Orchestrates the M3.3 end-to-end verification pipeline. Delegates entirely to the existing
/// publishing/package/contract/fingerprint machinery (<see cref="ISolutionPublishingOrchestrator"/>)
/// rather than re-implementing it; adds only the stages that did not previously exist anywhere
/// in the platform (solution-level test execution, deployment-plan-generation smoke check,
/// manifest identity consistency), and aggregates everything into one report.
/// </summary>
public interface IPlatformVerificationPipeline
{
    Task<PlatformVerificationReport> VerifyAsync(
        PlatformVerificationRequest request,
        CancellationToken cancellationToken = default);
}
