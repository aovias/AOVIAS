using Aovias.Configuration.Settings;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Packaging;
using Aovias.Publishing.Core.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Pipeline;

/// <summary>
/// Resolves project type providers.
/// </summary>
public sealed class ProjectTypeProviderRegistry : IProjectTypeProviderRegistry
{
    private readonly IReadOnlyList<IProjectTypeProvider> providers;

    public ProjectTypeProviderRegistry(IEnumerable<IProjectTypeProvider> providers)
    {
        this.providers = providers.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IProjectTypeProvider> Providers => providers;

    /// <inheritdoc />
    public async Task<ProjectDiscoveryResult?> DiscoverAsync(string projectPath, CancellationToken cancellationToken)
    {
        ProjectDiscoveryResult? best = null;
        foreach (var provider in providers)
        {
            if (!provider.CanDiscover(projectPath))
            {
                continue;
            }

            var discovery = await provider.DiscoverAsync(projectPath, cancellationToken);
            if (discovery is null)
            {
                continue;
            }

            if (best is null || discovery.Confidence > best.Confidence)
            {
                best = discovery;
            }
        }

        return best;
    }

    /// <inheritdoc />
    public IProjectTypeProvider Resolve(ProjectDiscoveryResult discovery)
        => providers.First(provider => string.Equals(provider.ProviderName, discovery.ProviderName, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Thin façade over <see cref="IPublishingPipelineOrchestrator"/> plus deferred DRV completion.
/// </summary>
public sealed class PublishingPipeline : IPublishingPipeline
{
    private readonly IPublishingPipelineOrchestrator orchestrator;
    private readonly IDeployPackageBuilder deployPackageBuilder;
    private readonly IPackageVerifier packageVerifier;
    private readonly IDeployPackageReader deployPackageReader;
    private readonly IDeploymentValidator deploymentValidator;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<PublishingPipeline> logger;

    public PublishingPipeline(
        IPublishingPipelineOrchestrator orchestrator,
        IDeployPackageBuilder deployPackageBuilder,
        IPackageVerifier packageVerifier,
        IDeployPackageReader deployPackageReader,
        IDeploymentValidator deploymentValidator,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<PublishingPipeline> logger)
    {
        this.orchestrator = orchestrator;
        this.deployPackageBuilder = deployPackageBuilder;
        this.packageVerifier = packageVerifier;
        this.deployPackageReader = deployPackageReader;
        this.deploymentValidator = deploymentValidator;
        this.settings = settings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task<PublishingResult> ExecuteAsync(
        PublishingRequest request,
        IProgress<PublishingProgress>? progress,
        CancellationToken cancellationToken)
        => orchestrator.ExecuteAsync(request, progress, request.ExecutionOptions, cancellationToken);

    /// <inheritdoc />
    public async Task<PublishingResult> CompleteDeferredDeploymentReadinessAsync(
        PendingPublishValidation pending,
        IProgress<PublishingProgress>? progress = null,
        CancellationToken cancellationToken = default,
        Action? sampleResourceUsage = null,
        Action<long>? recordFreedBytes = null)
    {
        var context = await RestoreContextFromPendingAsync(pending, progress, cancellationToken);
        try
        {
            if (!settings.CurrentValue.EnableDeploymentReadinessValidation)
            {
                var minimalReport = DeploymentReadinessReport.Ready([], TimeSpan.Zero);
                PublishingStagingArtifacts.EnsureServiceImageTarInStaging(pending);
                var finalizeMinimal = await deployPackageBuilder.FinalizeAsync(context, minimalReport, cancellationToken);
                if (!finalizeMinimal.IsSuccess)
                {
                    return PublishingResult.Failure(finalizeMinimal.Message);
                }

                return await BuildSuccessResultAsync(context, cancellationToken);
            }

            var report = await RunStreamingDeploymentValidationAsync(
                context,
                pending,
                sampleResourceUsage,
                recordFreedBytes,
                cancellationToken);
            context.ReadinessReport = report;
            context.Warnings.AddRange(report.Warnings);

            if (!report.IsReady)
            {
                return PublishingResult.Failure(string.Join("; ", report.Errors), report);
            }

            PublishingStagingArtifacts.EnsureServiceImageTarInStaging(pending);

            var finalize = await deployPackageBuilder.FinalizeAsync(context, report, cancellationToken);
            if (!finalize.IsSuccess)
            {
                return PublishingResult.Failure(finalize.Message, report);
            }

            var verify = await VerifyFinalPackageAsync(context, cancellationToken);
            if (!verify.IsSuccess)
            {
                return PublishingResult.Failure(verify.Message, report);
            }

            return await BuildSuccessResultAsync(context, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deferred deployment readiness failed for {ServiceId}.", pending.ServiceId);
            return PublishingResult.Failure(exception.Message, context.ReadinessReport);
        }
        finally
        {
            TryDeleteWorkingDirectory(pending.WorkingDirectory, recordFreedBytes);
        }
    }

    private async Task<DeploymentReadinessReport> RunStreamingDeploymentValidationAsync(
        PublishingContext context,
        PendingPublishValidation pending,
        Action? sampleResourceUsage,
        Action<long>? recordFreedBytes,
        CancellationToken cancellationToken)
    {
        sampleResourceUsage?.Invoke();
        var contents = await deployPackageReader.ReadAsync(pending.PackagePath, cancellationToken);
        if (contents is null)
        {
            throw new InvalidOperationException("Unable to read deployment package for readiness validation.");
        }

        var validationRoot = settings.CurrentValue.DeploymentValidationFolder;
        Directory.CreateDirectory(validationRoot);

        var drvContext = new DeploymentValidationContext
        {
            PackagePath = pending.PackagePath,
            PackageContents = contents,
            DeploymentContracts = contents.DeploymentContracts,
            WorkingDirectory = validationRoot,
            StagingDirectory = pending.StagingDirectory,
            ServiceImageTarPath = pending.ServiceImageTarPath,
            ValidationConfigRoot = settings.CurrentValue.ValidationConfigRoot,
            ValidationProfile = settings.CurrentValue.ValidationProfile,
            ExpectedDockerNetwork = settings.CurrentValue.DefaultNetworkName,
            ContainerPort = pending.ContainerPort,
            DockerExecutable = settings.CurrentValue.DockerExecutable,
            TimeoutSeconds = settings.CurrentValue.DeploymentValidationTimeoutSeconds,
            ValidationMode = ParseValidationMode(pending.ValidationMode),
            ExecutionScope = ValidationExecutionScope.Publishing,
            UseStreamingValidation = true,
            ResourceMonitor = new PublishingResourceMonitorAdapter(validationRoot, sampleResourceUsage, recordFreedBytes),
            ReportProgress = (stageName, status, message, percent) =>
                context.Report($"DRV: {stageName}", status, message, percent)
        };

        return await deploymentValidator.ValidateAsync(drvContext, cancellationToken);
    }

    private async Task<PublishingResult> BuildSuccessResultAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        var packageContents = await deployPackageReader.ReadAsync(context.PackagePath!, cancellationToken);
        PublishingReportEmission.EmitSibling(context, logger);
        context.Report("Completed", "Success", "Publishing Ready.", 100);
        return PublishingResult.Success(
            context.PackagePath!,
            packageContents,
            context.GeneratedFiles,
            context.ReadinessReport,
            context.Warnings,
            QualityGateEvaluator.Evaluate(context),
            context.StageTimings.ToList());
    }

    private async Task<PublishingContext> RestoreContextFromPendingAsync(
        PendingPublishValidation pending,
        IProgress<PublishingProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var staging = pending.StagingDirectory;
        var manifestJson = await File.ReadAllTextAsync(Path.Combine(staging, DeployPackageLayout.ManifestFile), cancellationToken);
        var healthJson = await File.ReadAllTextAsync(Path.Combine(staging, DeployPackageLayout.HealthFile), cancellationToken);
        var metadataJson = await File.ReadAllTextAsync(Path.Combine(staging, DeployPackageLayout.MetadataFile), cancellationToken);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = pending.ProjectPath,
                ServiceId = pending.ServiceId,
                Version = pending.Version,
                OutputDirectory = Path.GetDirectoryName(pending.PackagePath),
                Profile = pending.Profile,
                Publisher = pending.Publisher
            },
            Progress = progress,
            WorkingDirectory = pending.WorkingDirectory,
            StagingDirectory = staging,
            PublishOutputDirectory = Path.Combine(pending.WorkingDirectory, "publish"),
            PackagePath = pending.PackagePath,
            InterimPackagePath = pending.PackagePath,
            DockerImageTarPath = pending.ServiceImageTarPath,
            Manifest = System.Text.Json.JsonSerializer.Deserialize<DeployManifest>(manifestJson, jsonOptions),
            Health = System.Text.Json.JsonSerializer.Deserialize<DeployHealthDefinition>(healthJson, jsonOptions),
            Metadata = System.Text.Json.JsonSerializer.Deserialize<DeployPackageMetadata>(metadataJson, jsonOptions)
        };

        if (File.Exists(Path.Combine(staging, DeployPackageLayout.ComposeFile)))
        {
            context.ComposeContent = await File.ReadAllTextAsync(Path.Combine(staging, DeployPackageLayout.ComposeFile), cancellationToken);
        }

        if (File.Exists(Path.Combine(staging, DeployPackageLayout.EnvTemplateFile)))
        {
            context.EnvTemplateContent = await File.ReadAllTextAsync(Path.Combine(staging, DeployPackageLayout.EnvTemplateFile), cancellationToken);
        }

        return context;
    }

    private static DeploymentValidationMode ParseValidationMode(string? mode)
        => Enum.TryParse<DeploymentValidationMode>(mode, ignoreCase: true, out var parsed)
            ? parsed
            : DeploymentValidationMode.Balanced;

    private static void TryDeleteWorkingDirectory(string? workingDirectory, Action<long>? recordFreedBytes = null)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
        {
            return;
        }

        var bytes = DrvResourceMonitor.GetDirectorySizeBytes(workingDirectory);
        try
        {
            Directory.Delete(workingDirectory, recursive: true);
            recordFreedBytes?.Invoke(bytes);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private sealed class PublishingResourceMonitorAdapter : DrvResourceMonitor
    {
        private readonly Action? sampleCallback;
        private readonly Action<long>? recordFreedCallback;

        public PublishingResourceMonitorAdapter(
            string probePath,
            Action? sampleCallback,
            Action<long>? recordFreedCallback)
            : base(probePath)
        {
            this.sampleCallback = sampleCallback;
            this.recordFreedCallback = recordFreedCallback;
        }

        public override void Sample() => sampleCallback?.Invoke();

        public override void RecordFreed(long bytes) => recordFreedCallback?.Invoke(bytes);
    }

    private async Task<PublishingStepResult> VerifyFinalPackageAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        var report = await packageVerifier.VerifyAsync(
            context.PackagePath!,
            expectCertificate: true,
            expectDeterministicFingerprint: true,
            cancellationToken);
        context.PackageVerificationReport = report;
        context.PackageVerification = report.IsValid
            ? DeployPackageVerificationResult.Success(report.VerifiedFiles, report.ChecksumCount, report.HasDeploymentCertificate)
            : DeployPackageVerificationResult.Failure(report.Errors);
        return report.IsValid
            ? PublishingStepResult.Success("Final package verified (certificate present, checksums consistent).")
            : PublishingStepResult.Failure($"Final package verification failed: {string.Join("; ", report.Errors)}");
    }
}
