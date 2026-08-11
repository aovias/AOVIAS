using System.Diagnostics;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Verification;

/// <summary>
/// Sprint M3.3 — End-to-End Verification Pipeline: Build -> Test -> Publish -> Package -> Verification.
///
/// This type adds no publishing, packaging, or contract-generation logic of its own. It
/// composes three things that already exist (<see cref="IDotNetCliExecutor"/> for solution-level
/// build/test, <see cref="ISolutionPublishingOrchestrator"/> for publish/package/fingerprint/
/// contract, <see cref="IDeployPackageReader"/> to read published packages back for cross-checks)
/// with the two checks that had no home anywhere in the platform before this sprint
/// (<see cref="PackageManifestConsistencyValidator"/> and <see cref="DeploymentPlanGenerationVerifier"/>),
/// and emits one <see cref="PlatformVerificationReport"/> per run.
/// </summary>
public sealed class PlatformVerificationPipeline : IPlatformVerificationPipeline
{
    private readonly IDotNetCliExecutor dotNetCliExecutor;
    private readonly ISolutionPublishingOrchestrator solutionPublishingOrchestrator;
    private readonly IDeployPackageReader deployPackageReader;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<PlatformVerificationPipeline> logger;

    public PlatformVerificationPipeline(
        IDotNetCliExecutor dotNetCliExecutor,
        ISolutionPublishingOrchestrator solutionPublishingOrchestrator,
        IDeployPackageReader deployPackageReader,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<PlatformVerificationPipeline> logger)
    {
        this.dotNetCliExecutor = dotNetCliExecutor;
        this.solutionPublishingOrchestrator = solutionPublishingOrchestrator;
        this.deployPackageReader = deployPackageReader;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<PlatformVerificationReport> VerifyAsync(
        PlatformVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.StartNew();
        var platformRoot = PlatformRootResolver.Resolve(request.PlatformRoot);
        var solutionPath = request.SolutionPath ?? Path.Combine(platformRoot, "Aovias.Platform.slnx");

        var (buildStatus, testStatus) = await RunSolutionBuildAndTestAsync(request, solutionPath, platformRoot, cancellationToken);

        SolutionPublishingResult solutionResult;
        VerificationStageStatus publishStatus;
        var projectReports = new List<ProjectVerificationReport>();

        if (buildStatus.Outcome == VerificationOutcome.Failed)
        {
            // Deterministic ordering: a proven solution-build failure means the code does not
            // compile, so publishing (which builds each project again, independently) would only
            // waste time and produce confusing per-project noise. Blocked/Skipped are different —
            // those mean "unknown," not "proven broken," so publish still runs and stands on its
            // own evidence (its own dotnet invocations, per project, via the existing orchestrator).
            solutionResult = new SolutionPublishingResult
            {
                IsSuccess = false,
                Message = "Publish not attempted: solution build failed."
            };
            publishStatus = VerificationStageStatus.Skipped(
                "Solution build failed; publish not attempted to avoid wasted work against non-compiling code.");
        }
        else
        {
            solutionResult = await solutionPublishingOrchestrator.PublishSolutionAsync(
                new SolutionPublishingRequest
                {
                    PlatformRoot = request.PlatformRoot,
                    Profile = request.Profile,
                    Version = request.Version,
                    OutputDirectory = request.OutputDirectory,
                    Publisher = request.Publisher
                },
                progress: null,
                cancellationToken);

            publishStatus = solutionResult.IsSuccess
                ? VerificationStageStatus.Passed(
                    solutionResult.Message,
                    VerificationEvidenceTier.RuntimeVerification)
                : VerificationStageStatus.Failed(
                    "Solution publish did not complete successfully.",
                    solutionResult.Message,
                    VerificationEvidenceTier.RuntimeVerification);

            foreach (var projectResult in solutionResult.ProjectResults)
            {
                projectReports.Add(await BuildProjectReportAsync(projectResult, request.Version, cancellationToken));
            }
        }

        var packageValidation = AggregateStage(projectReports.Select(report => report.PackageValidation), "Package generation");
        var fingerprintValidation = AggregateStage(projectReports.Select(report => report.FingerprintValidation), "Package fingerprint verification");
        var contractValidation = AggregateStage(projectReports.Select(report => report.ContractValidation), "Infrastructure contract generation");
        var manifestValidation = AggregateStage(projectReports.Select(report => report.ManifestValidation), "Package manifest consistency");
        var deploymentPlanValidation = AggregateStage(projectReports.Select(report => report.DeploymentPlanValidation), "Deployment plan generation");

        // Sprint M3.4: deploy-time lifecycle readiness. RuntimeStartup/ReconciliationInitialization
        // are pure source-wiring checks and always run; Health/Certification/Rollback need a real,
        // already-published bundle to build a representative DeploymentPlan against.
        var runtimeStartupValidation = DeploymentLifecycleReadinessVerifier.VerifyRuntimeStartupWiring(platformRoot);
        var reconciliationInitializationValidation = DeploymentLifecycleReadinessVerifier.VerifyReconciliationInitializationWiring(platformRoot);
        var (healthGateValidation, deploymentCertificationValidation, rollbackReadinessValidation) =
            await VerifyDeployTimeLifecycleAsync(solutionResult, cancellationToken);

        var overallResult =
            IsAcceptable(buildStatus) && IsAcceptable(testStatus) && publishStatus.IsPassed
            && IsAcceptable(packageValidation) && IsAcceptable(fingerprintValidation)
            && IsAcceptable(contractValidation) && IsAcceptable(manifestValidation)
            && IsAcceptable(deploymentPlanValidation)
            && IsAcceptable(runtimeStartupValidation) && IsAcceptable(healthGateValidation)
            && IsAcceptable(deploymentCertificationValidation) && IsAcceptable(rollbackReadinessValidation)
            && IsAcceptable(reconciliationInitializationValidation)
            && projectReports.All(report => report.OverallResult);

        started.Stop();

        var report = new PlatformVerificationReport
        {
            BuildStatus = buildStatus,
            TestStatus = testStatus,
            PublishStatus = publishStatus,
            PackageValidation = packageValidation,
            FingerprintValidation = fingerprintValidation,
            ContractValidation = contractValidation,
            ManifestValidation = manifestValidation,
            DeploymentPlanValidation = deploymentPlanValidation,
            RuntimeStartupValidation = runtimeStartupValidation,
            HealthGateValidation = healthGateValidation,
            DeploymentCertificationValidation = deploymentCertificationValidation,
            RollbackReadinessValidation = rollbackReadinessValidation,
            ReconciliationInitializationValidation = reconciliationInitializationValidation,
            ProjectReports = projectReports,
            OverallResult = overallResult,
            GeneratedUtc = DateTimeOffset.UtcNow,
            TotalDuration = started.Elapsed
        };

        return WriteReport(report, request);
    }

    private async Task<(VerificationStageStatus Health, VerificationStageStatus Certification, VerificationStageStatus Rollback)>
        VerifyDeployTimeLifecycleAsync(SolutionPublishingResult solutionResult, CancellationToken cancellationToken)
    {
        var representative = solutionResult.ProjectResults
            .FirstOrDefault(result => result.IsSuccess && !string.IsNullOrWhiteSpace(result.PackagePath));

        if (representative is null)
        {
            var skipped = VerificationStageStatus.Skipped(
                "No successfully published package available to build a representative deployment plan.");
            return (skipped, skipped, skipped);
        }

        DeployPackageContents? contents;
        try
        {
            contents = await deployPackageReader.ReadAsync(representative.PackagePath!, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read back representative package for deploy-time lifecycle checks.");
            var failed = VerificationStageStatus.Failed(
                "Unable to read the representative package for deploy-time lifecycle checks.",
                exception.Message,
                VerificationEvidenceTier.RuntimeVerification);
            return (failed, failed, failed);
        }

        if (contents?.DeploymentContracts is null)
        {
            var skipped = VerificationStageStatus.Skipped(
                "Representative package carries no deployment contracts; cannot build a deployment plan.");
            return (skipped, skipped, skipped);
        }

        var plan = DeploymentLifecycleReadinessVerifier.BuildRepresentativePlan(contents.DeploymentContracts, out var failureReason);
        return (
            DeploymentLifecycleReadinessVerifier.VerifyHealthGateReadiness(plan, failureReason),
            DeploymentLifecycleReadinessVerifier.VerifyDeploymentCertificationReadiness(plan, failureReason),
            DeploymentLifecycleReadinessVerifier.VerifyRollbackReadiness(plan, failureReason));
    }

    private async Task<(VerificationStageStatus Build, VerificationStageStatus Test)> RunSolutionBuildAndTestAsync(
        PlatformVerificationRequest request,
        string solutionPath,
        string platformRoot,
        CancellationToken cancellationToken)
    {
        if (!request.VerifySolutionBuildAndTest)
        {
            var skipped = VerificationStageStatus.Skipped("Solution build/test verification not requested for this run.");
            return (skipped, skipped);
        }

        ProcessExecutionResult inspect;
        try
        {
            inspect = await dotNetCliExecutor.InspectAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "dotnet SDK inspection threw before build/test could run.");
            var blocked = VerificationStageStatus.Blocked($"dotnet SDK is not available in this environment: {exception.Message}");
            return (blocked, blocked);
        }

        if (!inspect.IsSuccess)
        {
            var blocked = VerificationStageStatus.Blocked(
                $"dotnet SDK is not available in this environment (exit code {inspect.ExitCode}). "
                + "Per the Execution Honesty Rule (IMPLEMENTATION_GUIDELINES.md §11), build/test results are not fabricated.");
            return (blocked, blocked);
        }

        if (!File.Exists(solutionPath))
        {
            var missing = VerificationStageStatus.Failed(
                "Consolidated CI solution not found.",
                $"Expected solution at '{solutionPath}'.",
                VerificationEvidenceTier.StaticVerification);
            return (missing, VerificationStageStatus.Skipped("Build did not succeed; tests not attempted."));
        }

        var restore = await dotNetCliExecutor.RestoreAsync(solutionPath, platformRoot, cancellationToken);
        if (!restore.IsSuccess)
        {
            var failed = VerificationStageStatus.Failed(
                "dotnet restore failed.",
                restore.StandardError,
                VerificationEvidenceTier.RuntimeVerification);
            return (failed, VerificationStageStatus.Skipped("Build did not succeed; tests not attempted."));
        }

        var build = await dotNetCliExecutor.BuildAsync(solutionPath, platformRoot, cancellationToken);
        var buildStatus = build.IsSuccess
            ? VerificationStageStatus.Passed("dotnet build succeeded for Aovias.Platform.slnx.", VerificationEvidenceTier.RuntimeVerification)
            : VerificationStageStatus.Failed("dotnet build failed.", build.StandardError, VerificationEvidenceTier.RuntimeVerification);

        if (!build.IsSuccess)
        {
            return (buildStatus, VerificationStageStatus.Skipped("Build did not succeed; tests not attempted."));
        }

        var test = await dotNetCliExecutor.TestAsync(solutionPath, platformRoot, cancellationToken);
        var testStatus = test.IsSuccess
            ? VerificationStageStatus.Passed("dotnet test succeeded for Aovias.Platform.slnx.", VerificationEvidenceTier.RuntimeVerification)
            : VerificationStageStatus.Failed("dotnet test reported failing tests.", test.StandardError, VerificationEvidenceTier.RuntimeVerification);

        return (buildStatus, testStatus);
    }

    private async Task<ProjectVerificationReport> BuildProjectReportAsync(
        SolutionProjectPublishResult projectResult,
        string requestedVersion,
        CancellationToken cancellationToken)
    {
        var serviceId = projectResult.Project.ServiceId;
        var pipelineState = projectResult.PublishingResult?.PipelineState;

        var packageValidation = StageFromPipeline(pipelineState, PublishingPipelineStageId.PackageGeneration, "Package generation");
        var fingerprintValidation = StageFromPipeline(pipelineState, PublishingPipelineStageId.PackageVerification, "Package fingerprint verification");
        var contractValidation = StageFromPipeline(pipelineState, PublishingPipelineStageId.ContractGeneration, "Infrastructure contract generation");

        if (!projectResult.IsSuccess || string.IsNullOrWhiteSpace(projectResult.PackagePath))
        {
            var unavailable = VerificationStageStatus.Skipped($"No package was produced for '{serviceId}'; manifest/plan checks not applicable.");
            return new ProjectVerificationReport
            {
                ServiceId = serviceId,
                PackageValidation = packageValidation,
                FingerprintValidation = fingerprintValidation,
                ContractValidation = contractValidation,
                ManifestValidation = unavailable,
                DeploymentPlanValidation = unavailable
            };
        }

        DeployPackageContents? contents;
        try
        {
            contents = await deployPackageReader.ReadAsync(projectResult.PackagePath, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read back package for '{ServiceId}' during verification.", serviceId);
            var readFailed = VerificationStageStatus.Failed(
                "Unable to read the published package back for verification.",
                exception.Message,
                VerificationEvidenceTier.RuntimeVerification);
            return new ProjectVerificationReport
            {
                ServiceId = serviceId,
                PackageValidation = packageValidation,
                FingerprintValidation = fingerprintValidation,
                ContractValidation = contractValidation,
                ManifestValidation = readFailed,
                DeploymentPlanValidation = readFailed
            };
        }

        if (contents is null)
        {
            var unreadable = VerificationStageStatus.Failed(
                "Package could not be parsed for verification.",
                $"'{projectResult.PackagePath}' did not yield readable package contents.",
                VerificationEvidenceTier.RuntimeVerification);
            return new ProjectVerificationReport
            {
                ServiceId = serviceId,
                PackageValidation = packageValidation,
                FingerprintValidation = fingerprintValidation,
                ContractValidation = contractValidation,
                ManifestValidation = unreadable,
                DeploymentPlanValidation = unreadable
            };
        }

        var manifestValidation = PackageManifestConsistencyValidator.Validate(contents, serviceId, requestedVersion);
        var deploymentPlanValidation = DeploymentPlanGenerationVerifier.Verify(contents);

        return new ProjectVerificationReport
        {
            ServiceId = serviceId,
            PackageValidation = packageValidation,
            FingerprintValidation = fingerprintValidation,
            ContractValidation = contractValidation,
            ManifestValidation = manifestValidation,
            DeploymentPlanValidation = deploymentPlanValidation
        };
    }

    private static VerificationStageStatus StageFromPipeline(
        PublishingPipelineState? pipelineState,
        PublishingPipelineStageId stageId,
        string label)
    {
        var stage = pipelineState?.GetStage(stageId);
        if (stage is null)
        {
            return VerificationStageStatus.Skipped($"{label} stage did not run.");
        }

        return stage.State switch
        {
            PublishingStageState.Passed or PublishingStageState.Warning =>
                VerificationStageStatus.Passed($"{label} passed.", VerificationEvidenceTier.RuntimeVerification, stage.Warnings),
            PublishingStageState.Skipped =>
                VerificationStageStatus.Skipped($"{label} was skipped."),
            _ => VerificationStageStatus.Failed(
                $"{label} failed.",
                stage.Errors.FirstOrDefault() ?? "Unknown failure.",
                VerificationEvidenceTier.RuntimeVerification,
                stage.Errors)
        };
    }

    private static VerificationStageStatus AggregateStage(IEnumerable<VerificationStageStatus> perProject, string label)
    {
        var statuses = perProject.ToList();
        if (statuses.Count == 0)
        {
            return VerificationStageStatus.Skipped($"{label}: no projects to verify.");
        }

        var failures = statuses.Where(status => status.Outcome == VerificationOutcome.Failed).ToList();
        if (failures.Count > 0)
        {
            return VerificationStageStatus.Failed(
                $"{label} failed for {failures.Count} of {statuses.Count} project(s).",
                string.Join("; ", failures.Select(failure => failure.RootCause ?? failure.Message)),
                VerificationEvidenceTier.RuntimeVerification);
        }

        var blocked = statuses.Where(status => status.Outcome == VerificationOutcome.Blocked).ToList();
        if (blocked.Count > 0)
        {
            return VerificationStageStatus.Blocked($"{label} blocked for {blocked.Count} of {statuses.Count} project(s).");
        }

        return statuses.All(status => status.Outcome == VerificationOutcome.Skipped)
            ? VerificationStageStatus.Skipped($"{label}: skipped for all {statuses.Count} project(s).")
            : VerificationStageStatus.Passed($"{label} passed for all applicable projects ({statuses.Count} total).", VerificationEvidenceTier.RuntimeVerification);
    }

    private static bool IsAcceptable(VerificationStageStatus status)
        => status.Outcome is VerificationOutcome.Passed or VerificationOutcome.Skipped;

    private PlatformVerificationReport WriteReport(PlatformVerificationReport report, PlatformVerificationRequest request)
    {
        try
        {
            var outputDirectory = request.OutputDirectory ?? settings.CurrentValue.OutputFolder;
            Directory.CreateDirectory(outputDirectory);
            var reportPath = Path.Combine(outputDirectory, $"VerificationReport-{report.GeneratedUtc:yyyyMMddTHHmmssZ}.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return report with { ReportPath = reportPath };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to write verification report to disk; returning in-memory report only.");
            return report;
        }
    }
}
