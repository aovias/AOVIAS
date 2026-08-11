using System.Diagnostics;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Executes deployment readiness validation stages in sequence.
/// </summary>
public sealed class DeploymentValidationEngine : IDeploymentValidator
{
    private static readonly SemaphoreSlim ValidationGate = new(1, 1);
    private readonly IEnumerable<IDeploymentValidationStage> stages;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<DeploymentValidationEngine> logger;

    public DeploymentValidationEngine(
        IEnumerable<IDeploymentValidationStage> stages,
        ILoggerFactory loggerFactory,
        ILogger<DeploymentValidationEngine> logger)
    {
        this.stages = stages;
        this.loggerFactory = loggerFactory;
        this.logger = logger;
    }

    public async Task<DeploymentReadinessReport> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        await ValidationGate.WaitAsync(cancellationToken);
        try
        {
            return await ValidateCoreAsync(context, cancellationToken);
        }
        finally
        {
            ValidationGate.Release();
        }
    }

    private async Task<DeploymentReadinessReport> ValidateCoreAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var validationLogger = new DeploymentValidationLogger(
            loggerFactory.CreateLogger<DeploymentValidationLogger>());
        context.ValidationLogger = validationLogger;
        var started = Stopwatch.StartNew();
        validationLogger.Begin(context.WorkingDirectory ?? Path.GetTempPath());
        context.LogFilePath = validationLogger.LogFilePath;

        var stageResults = new List<ValidationStageResult>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var recommendations = new List<string>();
        var deferredRuntimeVerification = false;

        try
        {
            var stageIndex = 0;
            foreach (var stage in stages)
            {
                var descriptor = stage.Descriptor;

                if (ValidationScopePolicy.ShouldDefer(descriptor, context.ExecutionScope))
                {
                    deferredRuntimeVerification = true;
                    var deferredName = ValidationScopePolicy.MapDeferredStageName(stage.StageName);
                    var deferred = ValidationStageResult.Defer(
                        deferredName,
                        TimeSpan.Zero,
                        ValidationScopePolicy.DeferredReason(stage.StageName),
                        ["Runs during deployment."]);
                    stageResults.Add(deferred);
                    validationLogger.WriteStage(deferredName, deferred);
                    context.ReportProgress?.Invoke(
                        deferredName,
                        "Deferred",
                        ValidationScopePolicy.DeferredReason(stage.StageName),
                        91 + stageIndex);
                    stageIndex++;
                    continue;
                }

                if (!DeploymentValidationModePolicy.ShouldRunStage(stage.StageName, context.ValidationMode))
                {
                    continue;
                }

                if (!ValidationScopePolicy.ShouldExecute(descriptor, context.ExecutionScope))
                {
                    continue;
                }

                if (context.ExecutionScope == ValidationExecutionScope.Publishing
                    && descriptor.ExecutionScope == ValidationExecutionScope.Deployment)
                {
                    logger.LogWarning(
                        "Unexpected validator execution. Validator={Validator} CurrentScope=Publishing ExpectedScope=Deployment",
                        stage.StageName);
                }

                cancellationToken.ThrowIfCancellationRequested();
                logger.LogInformation(
                    "Running validation stage {Stage} for scope {Scope}",
                    stage.StageName,
                    context.ExecutionScope);
                context.ResourceMonitor?.Sample();

                var result = await stage.ValidateAsync(context, cancellationToken);
                stageResults.Add(result);
                validationLogger.WriteStage(stage.StageName, result);
                warnings.AddRange(result.Warnings);
                recommendations.AddRange(result.Recommendations);

                var percent = 91 + stageIndex;
                context.ReportProgress?.Invoke(
                    stage.StageName,
                    result.Deferred ? "Deferred" : result.Passed ? "Passed" : "Failed",
                    result.Passed
                        ? $"{stage.StageName} validation passed."
                        : string.Join("; ", result.Errors),
                    percent);
                stageIndex++;

                if (!result.Passed && !result.Deferred)
                {
                    errors.AddRange(result.Errors.Select(error => $"{stage.StageName}: {error}"));
                    break;
                }
            }

            if (deferredRuntimeVerification)
            {
                recommendations.Add("Runtime verification runs during deployment.");
            }
        }
        finally
        {
            if (context.Workspace is not null)
            {
                await context.Workspace.DisposeAsync();
                context.Workspace = null;
            }

            context.ResourceMonitor?.Sample();
        }

        started.Stop();
        var apiProbes = context.ApiProbes.ToList();
        var publishingScope = context.ExecutionScope == ValidationExecutionScope.Publishing;
        var allPassed = errors.Count == 0 && stageResults.All(stage => stage.Passed || stage.Deferred);
        var report = allPassed
            ? publishingScope
                ? DeploymentReadinessReport.PublishingReady(
                    stageResults,
                    started.Elapsed,
                    warnings,
                    recommendations,
                    validationLogger.LogFilePath,
                    apiProbes,
                    context.ContractValidationReport)
                : DeploymentReadinessReport.Ready(
                    stageResults,
                    started.Elapsed,
                    warnings,
                    recommendations,
                    validationLogger.LogFilePath,
                    apiProbes,
                    context.ContractValidationReport)
            : publishingScope
                ? DeploymentReadinessReport.NotPublishingReady(
                    stageResults,
                    started.Elapsed,
                    errors,
                    warnings,
                    recommendations,
                    validationLogger.LogFilePath,
                    apiProbes,
                    context.ContractValidationReport)
                : DeploymentReadinessReport.NotReady(
                    stageResults,
                    started.Elapsed,
                    errors,
                    warnings,
                    recommendations,
                    validationLogger.LogFilePath,
                    apiProbes,
                    context.ContractValidationReport);

        validationLogger.Complete(report);
        return report;
    }
}
