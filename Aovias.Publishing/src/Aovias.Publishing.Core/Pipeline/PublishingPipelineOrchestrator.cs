using System.Diagnostics;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Execution;
using Aovias.Publishing.Core.Packaging;
using Aovias.Publishing.Core.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Aovias.Publishing.Core.Pipeline;

/// <summary>
/// Stage-based publishing orchestrator with isolation and structured diagnostics.
/// </summary>
public sealed class PublishingPipelineOrchestrator : IPublishingPipelineOrchestrator
{
    private readonly PublishingStageExecutor stageExecutor;
    private readonly IPublishingWorkspaceManager workspaceManager;
    private readonly IPublishingLogWriter logWriter;
    private readonly IDeployPackageReader deployPackageReader;
    private readonly IGoldenPackageRepository goldenPackageRepository;
    private readonly IDockerCliExecutor dockerCliExecutor;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<PublishingPipelineOrchestrator> logger;

    public PublishingPipelineOrchestrator(
        PublishingStageExecutor stageExecutor,
        IPublishingWorkspaceManager workspaceManager,
        IPublishingLogWriter logWriter,
        IDeployPackageReader deployPackageReader,
        IGoldenPackageRepository goldenPackageRepository,
        IDockerCliExecutor dockerCliExecutor,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<PublishingPipelineOrchestrator> logger)
    {
        this.stageExecutor = stageExecutor;
        this.workspaceManager = workspaceManager;
        this.logWriter = logWriter;
        this.deployPackageReader = deployPackageReader;
        this.goldenPackageRepository = goldenPackageRepository;
        this.dockerCliExecutor = dockerCliExecutor;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<PublishingResult> ExecuteAsync(
        PublishingRequest request,
        IProgress<PublishingProgress>? progress = null,
        PublishingExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= request.ExecutionOptions ?? new PublishingExecutionOptions();
        workspaceManager.CleanStaleWorkspaces(TimeSpan.FromDays(2));
        var baseSession = workspaceManager.CreateSession(request);
        var session = await PublishingEnvironmentProbe.EnrichAsync(baseSession, dockerCliExecutor, cancellationToken);

        var context = CreateContext(request, session, progress, options);
        workspaceManager.RecoverWorkspace(session);
        var retryPolicy = CreateRetryPolicy(context);
        var stageResults = new List<PublishingStageExecutionResult>();
        var pipelineStarted = Stopwatch.StartNew();
        GoldenPackageComparisonResult? goldenComparison = null;

        try
        {
            foreach (var stageId in ResolveStages(options))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var percent = CalculatePercent(stageId);
                ReportStageProgress(context, stageId, PublishingStageState.Running, $"{stageId} started.", percent);

                PublishingStageExecutionResult result;
                try
                {
                    result = await retryPolicy.ExecuteAsync(async () =>
                        await stageExecutor.ExecuteStageAsync(stageId, context, session, cancellationToken));
                }
                catch (Exception exception) when (PublishingRecoveryPolicy.TryRecover(context, stageId, exception, out var recoveryMessage))
                {
                    logWriter.Write(session, MapLogName(stageId), recoveryMessage);
                    result = await stageExecutor.ExecuteStageAsync(stageId, context, session, cancellationToken);
                }

                WriteStageLog(session, stageId, result, context);
                stageResults.Add(result);
                context.StageTimings.Add(new PublishingStageTiming(result.StageName, result.Duration, result.Passed));
                ReportStageProgress(context, stageId, result.State, result.Errors.FirstOrDefault() ?? $"{stageId} completed.", percent);

                if (result.State is PublishingStageState.Failed or PublishingStageState.Cancelled)
                {
                    return BuildFailureResult(session, context, stageResults, pipelineStarted.Elapsed, result);
                }
            }

            if (context.Request.DeferDeploymentReadinessValidation)
            {
                return await CompleteDeferredPackageAsync(context, session, stageResults, pipelineStarted.Elapsed, cancellationToken);
            }

            goldenComparison = context.PackageVerificationReport?.DeterministicFingerprint is not null && context.InterimPackagePath is not null
                ? await goldenPackageRepository.CompareAsync(
                    context.Manifest?.Service ?? context.Request.ServiceId ?? "service",
                    context.InterimPackagePath,
                    context.PackageVerificationReport.DeterministicFingerprint,
                    cancellationToken)
                : null;

            var pipelineState = BuildPipelineState(session, stageResults, pipelineStarted.Elapsed, success: true, certified: true);
            context.PipelineState = pipelineState;
            context.ReliabilityReport = PublishingReliabilityReportBuilder.Build(
                session,
                pipelineState,
                context,
                goldenComparison,
                packageIntegrityVerified: context.PackageVerificationReport?.IsValid == true,
                deterministicMatched: goldenComparison?.Matches != false);

            WriteSummaryLog(session, context);
            PublishingReportEmission.EmitSibling(context, logger);

            var packageContents = context.PackagePath is not null
                ? await deployPackageReader.ReadAsync(context.PackagePath, cancellationToken)
                : null;

            context.Report("Completed", "Success", "Deployment Ready.", 100);
            return PublishingResult.Success(
                context.PackagePath!,
                packageContents,
                context.GeneratedFiles,
                context.ReadinessReport,
                context.Warnings,
                QualityGateEvaluator.Evaluate(context),
                context.StageTimings.ToList(),
                session with { CompletedUtc = DateTimeOffset.UtcNow, PipelineState = pipelineState },
                pipelineState,
                context.ReliabilityReport);
        }
        catch (OperationCanceledException)
        {
            stageResults.Add(new PublishingStageExecutionResult
            {
                StageId = PublishingPipelineStageId.DeploymentCertification,
                StageName = "Cancelled",
                State = PublishingStageState.Cancelled,
                Duration = pipelineStarted.Elapsed,
                Errors = ["Publishing was cancelled."]
            });
            return BuildFailureResult(session, context, stageResults, pipelineStarted.Elapsed, stageResults.Last());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing orchestrator failed.");
            return PublishingResult.Failure(
                exception.Message,
                context.ReadinessReport,
                session,
                BuildPipelineState(session, stageResults, pipelineStarted.Elapsed, success: false, certified: false),
                context.ReliabilityReport);
        }
        finally
        {
            if (!request.DeferDeploymentReadinessValidation)
            {
                workspaceManager.DisposeSession(session);
            }
        }
    }

    private async Task<PublishingResult> CompleteDeferredPackageAsync(
        PublishingContext context,
        PublishingSession session,
        List<PublishingStageExecutionResult> stageResults,
        TimeSpan elapsed,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var manifest = context.Manifest ?? throw new InvalidOperationException("Manifest is required.");
        var outputDirectory = context.Request.OutputDirectory ?? settings.CurrentValue.OutputFolder;
        Directory.CreateDirectory(outputDirectory);
        var packagePath = Path.Combine(outputDirectory, PublishingImageNaming.GetPackageFileName(manifest.Service));
        if (File.Exists(packagePath))
        {
            File.Delete(packagePath);
        }

        File.Copy(context.InterimPackagePath!, packagePath, overwrite: true);
        context.PackagePath = packagePath;
        context.GeneratedFiles.Add(packagePath);

        var protectedTarPath = PublishingStagingArtifacts.PrepareProtectedServiceImageTar(context);
        var pending = new PendingPublishValidation
        {
            ServiceId = manifest.Service,
            ProjectPath = context.Request.ProjectPath,
            Version = manifest.Version,
            Profile = context.Request.Profile,
            Publisher = context.Request.Publisher,
            StagingDirectory = context.StagingDirectory!,
            WorkingDirectory = context.WorkingDirectory!,
            PackagePath = packagePath,
            ServiceImageTarPath = protectedTarPath,
            ContainerPort = context.Discovery?.ContainerPort ?? RuntimeAgentConventions.DefaultPort,
            ValidationMode = context.Request.DeploymentValidationMode ?? settings.CurrentValue.DeploymentValidationMode
        };

        var pipelineState = BuildPipelineState(session, stageResults, elapsed, success: true, certified: false);
        return new PublishingResult
        {
            IsSuccess = true,
            Message = "Package created. Deployment validation pending.",
            PackagePath = packagePath,
            PendingValidation = pending,
            GeneratedFiles = context.GeneratedFiles,
            Warnings = context.Warnings,
            StageTimings = context.StageTimings,
            Session = session,
            PipelineState = pipelineState
        };
    }

    private static PublishingContext CreateContext(
        PublishingRequest request,
        PublishingSession session,
        IProgress<PublishingProgress>? progress,
        PublishingExecutionOptions options)
        => new()
        {
            Request = request,
            Progress = progress,
            Session = session,
            ExecutionOptions = options,
            WorkingDirectory = session.WorkspaceRoot,
            StagingDirectory = Path.Combine(session.WorkspaceRoot, "staging"),
            PublishOutputDirectory = Path.Combine(session.WorkspaceRoot, "publish"),
            DockerBuildContext = Path.Combine(session.WorkspaceRoot, "docker-context")
        };

    private static IEnumerable<PublishingPipelineStageId> ResolveStages(PublishingExecutionOptions options)
    {
        var stages = PublishingStageExecutor.CanonicalOrder;
        if (options.OnlyStages.Count > 0)
        {
            stages = options.OnlyStages.ToList();
        }

        var started = options.StartFromStage is null;
        foreach (var stage in stages)
        {
            if (!started)
            {
                if (stage == options.StartFromStage)
                {
                    started = true;
                }
                else
                {
                    continue;
                }
            }

            yield return stage;
            if (options.StopAfterStage == stage)
            {
                yield break;
            }
        }
    }

    private PublishingResult BuildFailureResult(
        PublishingSession session,
        PublishingContext context,
        List<PublishingStageExecutionResult> stageResults,
        TimeSpan elapsed,
        PublishingStageExecutionResult failedStage)
    {
        var pipelineState = BuildPipelineState(session, stageResults, elapsed, success: false, certified: false);
        context.PipelineState = pipelineState;
        context.ReliabilityReport = PublishingReliabilityReportBuilder.Build(
            session,
            pipelineState,
            context,
            goldenComparison: null,
            packageIntegrityVerified: false,
            deterministicMatched: false);
        WriteSummaryLog(session, context);

        return PublishingResult.Failure(
            failedStage.Errors.FirstOrDefault() ?? $"{failedStage.StageName} failed.",
            context.ReadinessReport,
            session with { PipelineState = pipelineState },
            pipelineState,
            context.ReliabilityReport);
    }

    private static PublishingPipelineState BuildPipelineState(
        PublishingSession session,
        IReadOnlyList<PublishingStageExecutionResult> stages,
        TimeSpan elapsed,
        bool success,
        bool certified)
    {
        var failed = stages.LastOrDefault(stage => stage.State == PublishingStageState.Failed);
        return new PublishingPipelineState
        {
            SessionId = session.SessionId,
            Stages = stages,
            IsComplete = true,
            IsSuccessful = success,
            IsDeploymentCertified = certified,
            TerminalFailureCategory = failed?.FailureCategory,
            TerminalFailureStage = failed?.StageName,
            TotalDuration = elapsed
        };
    }

    /// <summary>
    /// Retries entire stages on any exception. Stages must be overwrite-idempotent (M1 N9).
    /// </summary>
    private AsyncRetryPolicy CreateRetryPolicy(PublishingContext context)
        => Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                settings.CurrentValue.DefaultRetryCount,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (_, _, retry, _) =>
                {
                    context.TotalRetryCount++;
                    logger.LogWarning("Publishing stage retry {Retry}", retry);
                });

    private static int CalculatePercent(PublishingPipelineStageId stageId)
        => stageId switch
        {
            PublishingPipelineStageId.Discovery => 5,
            PublishingPipelineStageId.Preflight => 8,
            PublishingPipelineStageId.Restore => 15,
            PublishingPipelineStageId.Build => 25,
            PublishingPipelineStageId.DockerBuild => 50,
            PublishingPipelineStageId.ComposeGeneration => 70,
            PublishingPipelineStageId.ConfigurationGeneration => 80,
            PublishingPipelineStageId.ContractGeneration => 86,
            PublishingPipelineStageId.PackageGeneration => 89,
            PublishingPipelineStageId.PackageVerification => 90,
            PublishingPipelineStageId.DeploymentReadinessValidation => 95,
            PublishingPipelineStageId.ReleaseLibrary => 98,
            PublishingPipelineStageId.DeploymentCertification => 100,
            _ => 50
        };

    private static void ReportStageProgress(
        PublishingContext context,
        PublishingPipelineStageId stageId,
        PublishingStageState state,
        string message,
        int percent)
        => context.Report(stageId.ToString(), state.ToString(), message, percent);

    private void WriteStageLog(PublishingSession session, PublishingPipelineStageId stageId, PublishingStageExecutionResult result, PublishingContext context)
    {
        var logName = MapLogName(stageId);
        var payload = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        logWriter.Write(session, logName, payload);
        if (stageId == PublishingPipelineStageId.Build)
        {
            logWriter.Write(session, "Build.log", string.Join(Environment.NewLine, context.BuildOutput));
        }
    }

    private void WriteSummaryLog(PublishingSession session, PublishingContext context)
    {
        if (context.ReliabilityReport is not null)
        {
            logWriter.Write(session, "Summary.log", JsonSerializer.Serialize(context.ReliabilityReport, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static string MapLogName(PublishingPipelineStageId stageId)
        => stageId switch
        {
            PublishingPipelineStageId.Build or PublishingPipelineStageId.Restore => "Build.log",
            PublishingPipelineStageId.DockerBuild => "Docker.log",
            PublishingPipelineStageId.ContractGeneration => "Contracts.log",
            PublishingPipelineStageId.ComposeGeneration or PublishingPipelineStageId.ConfigurationGeneration => "Compose.log",
            PublishingPipelineStageId.PackageGeneration => "Package.log",
            PublishingPipelineStageId.PackageVerification => "Verification.log",
            PublishingPipelineStageId.DeploymentReadinessValidation => "DRV.log",
            _ => "Summary.log"
        };
}
