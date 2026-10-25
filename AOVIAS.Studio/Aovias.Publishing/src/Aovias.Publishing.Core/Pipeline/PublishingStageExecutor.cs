using System.Diagnostics;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Validation;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Execution;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Packaging;
using Aovias.Publishing.Core.ReleaseLibrary;
using Aovias.Publishing.Core.Validation;
using Aovias.Publishing.Core.Verification;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Pipeline;

/// <summary>
/// Executes canonical Sprint 8D publishing stages.
/// </summary>
public sealed class PublishingStageExecutor
{
    public const string EngineVersion = PublishingVersionPolicy.EngineVersion;

    private readonly IProjectTypeProviderRegistry providerRegistry;
    private readonly IDockerCliExecutor dockerCliExecutor;
    private readonly IDockerfileGenerator dockerfileGenerator;
    private readonly IDeployManifestGenerator manifestGenerator;
    private readonly IHealthDefinitionGenerator healthDefinitionGenerator;
    private readonly IConfigTemplateGenerator configTemplateGenerator;
    private readonly IEnvTemplateGenerator envTemplateGenerator;
    private readonly IComposeGenerator composeGenerator;
    private readonly IDeployPackageBuilder deployPackageBuilder;
    private readonly IDeployPackageValidator deployPackageValidator;
    private readonly IPackageVerifier packageVerifier;
    private readonly IDeployPackageReader deployPackageReader;
    private readonly IDeploymentValidator deploymentValidator;
    private readonly IGoldenPackageRepository goldenPackageRepository;
    private readonly IRuntimeAgentPackageVerifier runtimeAgentPackageVerifier;
    private readonly PublishingBuildIdentityService buildIdentityService;
    private readonly IDockerImageIdentityResolver dockerImageIdentityResolver;
    private readonly IReleaseLibraryService releaseLibraryService;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<PublishingStageExecutor> logger;

    public PublishingStageExecutor(
        IProjectTypeProviderRegistry providerRegistry,
        IDockerCliExecutor dockerCliExecutor,
        IDockerfileGenerator dockerfileGenerator,
        IDeployManifestGenerator manifestGenerator,
        IHealthDefinitionGenerator healthDefinitionGenerator,
        IConfigTemplateGenerator configTemplateGenerator,
        IEnvTemplateGenerator envTemplateGenerator,
        IComposeGenerator composeGenerator,
        IDeployPackageBuilder deployPackageBuilder,
        IDeployPackageValidator deployPackageValidator,
        IPackageVerifier packageVerifier,
        IDeployPackageReader deployPackageReader,
        IDeploymentValidator deploymentValidator,
        IGoldenPackageRepository goldenPackageRepository,
        IRuntimeAgentPackageVerifier runtimeAgentPackageVerifier,
        PublishingBuildIdentityService buildIdentityService,
        IDockerImageIdentityResolver dockerImageIdentityResolver,
        IReleaseLibraryService releaseLibraryService,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<PublishingStageExecutor> logger)
    {
        this.providerRegistry = providerRegistry;
        this.dockerCliExecutor = dockerCliExecutor;
        this.dockerfileGenerator = dockerfileGenerator;
        this.manifestGenerator = manifestGenerator;
        this.healthDefinitionGenerator = healthDefinitionGenerator;
        this.configTemplateGenerator = configTemplateGenerator;
        this.envTemplateGenerator = envTemplateGenerator;
        this.composeGenerator = composeGenerator;
        this.deployPackageBuilder = deployPackageBuilder;
        this.deployPackageValidator = deployPackageValidator;
        this.packageVerifier = packageVerifier;
        this.deployPackageReader = deployPackageReader;
        this.deploymentValidator = deploymentValidator;
        this.goldenPackageRepository = goldenPackageRepository;
        this.runtimeAgentPackageVerifier = runtimeAgentPackageVerifier;
        this.buildIdentityService = buildIdentityService;
        this.dockerImageIdentityResolver = dockerImageIdentityResolver;
        this.releaseLibraryService = releaseLibraryService;
        this.settings = settings;
        this.logger = logger;
    }

    public static IReadOnlyList<PublishingPipelineStageId> CanonicalOrder { get; } =
    [
        PublishingPipelineStageId.Discovery,
        PublishingPipelineStageId.Preflight,
        PublishingPipelineStageId.Restore,
        PublishingPipelineStageId.Build,
        PublishingPipelineStageId.DockerBuild,
        PublishingPipelineStageId.ConfigurationGeneration,
        PublishingPipelineStageId.ContractGeneration,
        PublishingPipelineStageId.ComposeGeneration,
        PublishingPipelineStageId.PackageGeneration,
        PublishingPipelineStageId.PackageVerification,
        PublishingPipelineStageId.DeploymentReadinessValidation,
        PublishingPipelineStageId.ReleaseLibrary,
        PublishingPipelineStageId.DeploymentCertification
    ];

    public async Task<PublishingStageExecutionResult> ExecuteStageAsync(
        PublishingPipelineStageId stageId,
        PublishingContext context,
        PublishingSession session,
        CancellationToken cancellationToken)
    {
        var running = PublishingStageExecutionResult.CreateRunning(stageId, stageId.ToString());
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var (state, message, artifacts, warnings) = stageId switch
            {
                PublishingPipelineStageId.Discovery => await RunDiscoveryAsync(context, cancellationToken),
                PublishingPipelineStageId.Preflight => await RunPreflightAsync(context, cancellationToken),
                PublishingPipelineStageId.Restore => await RunRestoreAsync(context, cancellationToken),
                PublishingPipelineStageId.Build => await RunBuildAsync(context, cancellationToken),
                PublishingPipelineStageId.DockerBuild => await RunDockerBuildAsync(context, cancellationToken),
                PublishingPipelineStageId.ConfigurationGeneration => await RunConfigurationGenerationAsync(context, cancellationToken),
                PublishingPipelineStageId.ContractGeneration => await RunContractGenerationAsync(context, cancellationToken),
                PublishingPipelineStageId.ComposeGeneration => await RunComposeGenerationAsync(context, cancellationToken),
                PublishingPipelineStageId.PackageGeneration => await RunPackageGenerationAsync(context, cancellationToken),
                PublishingPipelineStageId.PackageVerification => await RunPackageVerificationAsync(context, cancellationToken),
                PublishingPipelineStageId.DeploymentReadinessValidation => await RunDrvAsync(context, cancellationToken),
                PublishingPipelineStageId.ReleaseLibrary => await RunReleaseLibraryAsync(context, cancellationToken),
                PublishingPipelineStageId.DeploymentCertification => await RunDeploymentCertificationAsync(context, cancellationToken),
                _ => (PublishingStageState.Failed, "Unknown stage.", new Dictionary<string, string>(), Array.Empty<string>())
            };

            stopwatch.Stop();
            var category = state == PublishingStageState.Failed
                ? PublishingFailureClassifier.Classify(stageId, message)
                : PublishingFailureCategory.None;

            return running with
            {
                State = warnings.Count > 0 && state == PublishingStageState.Passed ? PublishingStageState.Warning : state,
                CompletedUtc = DateTimeOffset.UtcNow,
                Duration = stopwatch.Elapsed,
                Inputs = [context.Request.ProjectPath],
                Outputs = artifacts.Values.ToList(),
                Artifacts = artifacts.Keys.ToList(),
                Warnings = warnings,
                Errors = state == PublishingStageState.Failed ? [message] : [],
                Recommendation = state == PublishingStageState.Failed
                    ? PublishingFailureClassifier.Recommend(category, stageId)
                    : null,
                RecoveryAction = state == PublishingStageState.Failed
                    ? PublishingFailureClassifier.RecoveryAction(category, stageId)
                    : null,
                FailureCategory = category
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(exception, "Publishing stage {StageId} failed.", stageId);
            var category = PublishingFailureClassifier.Classify(stageId, exception.Message);
            return running with
            {
                State = PublishingStageState.Failed,
                CompletedUtc = DateTimeOffset.UtcNow,
                Duration = stopwatch.Elapsed,
                Errors = [exception.Message],
                ExceptionType = exception.GetType().FullName,
                ExceptionMessage = exception.Message,
                FailureCategory = category,
                Recommendation = PublishingFailureClassifier.Recommend(category, stageId),
                RecoveryAction = PublishingFailureClassifier.RecoveryAction(category, stageId)
            };
        }
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunDiscoveryAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        context.Discovery = await providerRegistry.DiscoverAsync(context.Request.ProjectPath, cancellationToken);
        if (context.Discovery is null)
        {
            return (PublishingStageState.Failed, "Unable to discover project type.", new Dictionary<string, string>(), []);
        }

        try
        {
            context.DeploymentMetadata = DeploymentMetadataReader.LoadRequired(context.Discovery);
        }
        catch (InvalidOperationException exception)
        {
            return (PublishingStageState.Failed, exception.Message, new Dictionary<string, string>(), []);
        }

        return (PublishingStageState.Passed, $"Discovered {context.Discovery.ProjectType}.", new Dictionary<string, string>(), []);
    }

    private Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunPreflightAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var warnings = new List<string>();
        var outputDirectory = context.Request.OutputDirectory ?? settings.CurrentValue.OutputFolder;
        try
        {
            Directory.CreateDirectory(outputDirectory);
        }
        catch (Exception exception)
        {
            return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                (PublishingStageState.Failed, $"Output directory is not writable: {exception.Message}", new Dictionary<string, string>(), []));
        }

        if (!PublishingProjectPaths.Exists(context.Request.ProjectPath))
        {
            return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                (PublishingStageState.Failed, "Project path was not found.", new Dictionary<string, string>(), []));
        }

        if (string.IsNullOrWhiteSpace(context.Request.ServiceId) && context.Discovery is null)
        {
            warnings.Add("Service id was not supplied before discovery completed.");
        }

        return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
            (PublishingStageState.Passed, "Preflight checks passed.", new Dictionary<string, string>(), warnings));
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunRestoreAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        var provider = providerRegistry.Resolve(context.Discovery!);
        var result = await provider.RestoreDependenciesAsync(context, cancellationToken);
        return result.IsSuccess
            ? (PublishingStageState.Passed, result.Message, new Dictionary<string, string>(), context.Warnings.ToList())
            : (PublishingStageState.Failed, result.Message, new Dictionary<string, string>(), []);
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunBuildAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        var provider = providerRegistry.Resolve(context.Discovery!);
        foreach (var step in new Func<PublishingContext, CancellationToken, Task<PublishingStepResult>>[]
                 {
                     provider.ValidateAsync,
                     provider.BuildAsync,
                     provider.PublishAsync,
                     provider.InspectConfigurationAsync
                 })
        {
            var result = await step(context, cancellationToken);
            if (!result.IsSuccess)
            {
                return (PublishingStageState.Failed, result.Message, new Dictionary<string, string>(), []);
            }
        }

        return (PublishingStageState.Passed, "Build completed.", new Dictionary<string, string>(), context.Warnings.ToList());
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunDockerBuildAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        var inspect = await dockerCliExecutor.InspectAsync(cancellationToken);
        if (!inspect.IsSuccess)
        {
            return (PublishingStageState.Failed, FormatDockerUnavailableMessage(inspect), new Dictionary<string, string>(), []);
        }

        var dockerfileResult = await dockerfileGenerator.PrepareDockerfileAsync(context, cancellationToken);
        if (!dockerfileResult.IsSuccess)
        {
            return (PublishingStageState.Failed, dockerfileResult.Message, new Dictionary<string, string>(), []);
        }

        if (!string.IsNullOrWhiteSpace(context.DockerBuildContext)
            && !string.IsNullOrWhiteSpace(context.DockerfilePath)
            && File.Exists(context.DockerfilePath))
        {
            var dockerfileContent = await File.ReadAllTextAsync(context.DockerfilePath, cancellationToken);
            if (RuntimeAgentDockerContextStager.UsesMonorepoDockerfile(dockerfileContent))
            {
                try
                {
                    RuntimeAgentDockerContextStager.ValidateStagedContext(context.DockerBuildContext, dockerfileContent);
                }
                catch (DirectoryNotFoundException ex)
                {
                    return (PublishingStageState.Failed, ex.Message, new Dictionary<string, string>(), []);
                }
            }
        }

        var serviceId = context.Request.ServiceId ?? context.Discovery!.ServiceId;
        var version = context.Request.Version ?? PublishingVersionPolicy.DefaultPackageVersion;
        context.DockerImageName = PublishingImageNaming.GetImageName(serviceId);
        context.DockerImageTag = version;

        var identity = buildIdentityService.EstablishPreDocker(context);
        var buildArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SOURCE_REVISION"] = identity.SourceRevision,
            ["BUILD_UTC"] = identity.BuildUtc.ToString("O")
        };

        var build = await dockerCliExecutor.BuildAsync(
            context.DockerBuildContext!,
            context.DockerfilePath!,
            context.DockerImageName,
            context.DockerImageTag,
            cancellationToken,
            buildArgs,
            noCache: false);
        AppendOutput(context, build);
        if (!build.IsSuccess)
        {
            return (PublishingStageState.Failed, build.StandardError, new Dictionary<string, string>(), []);
        }

        var dockerImageIdentity = await dockerImageIdentityResolver.ResolveAsync(
            context.DockerImageName,
            context.DockerImageTag,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(dockerImageIdentity))
        {
            return (PublishingStageState.Failed, "Unable to resolve Docker image identity after build.", new Dictionary<string, string>(), []);
        }

        buildIdentityService.ApplyDockerImageIdentity(context, dockerImageIdentity);

        if (RuntimeAgentConventions.IsRuntimeAgent(serviceId, context.Discovery?.ProjectRoot))
        {
            var verification = await runtimeAgentPackageVerifier.VerifyImageAsync(
                context.DockerImageName,
                context.DockerImageTag,
                identity.SourceRevision,
                cancellationToken);
            if (!verification.Passed)
            {
                return (PublishingStageState.Failed, string.Join("; ", verification.Errors), new Dictionary<string, string>(), []);
            }
        }

        Directory.CreateDirectory(context.StagingDirectory!);
        var tarPath = Path.Combine(context.StagingDirectory!, DeployPackageLayout.ServiceImageTar);
        var save = await dockerCliExecutor.SaveAsync(context.DockerImageName, context.DockerImageTag, tarPath, cancellationToken);
        AppendOutput(context, save);
        if (!save.IsSuccess)
        {
            return (PublishingStageState.Failed, save.StandardError, new Dictionary<string, string>(), []);
        }

        context.DockerImageTarPath = tarPath;
        context.GeneratedFiles.Add(tarPath);
        return (PublishingStageState.Passed, "Docker image built and exported.", new Dictionary<string, string>
        {
            ["docker-image.tar"] = tarPath,
            ["docker-image-ref"] = $"{context.DockerImageName}:{context.DockerImageTag}",
            ["docker-image-identity"] = dockerImageIdentity
        }, []);
    }

    private Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunComposeGenerationAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
        => RunComposeOnlyAsync(context, cancellationToken);

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunComposeOnlyAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        await composeGenerator.GenerateAsync(context, cancellationToken);
        var artifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(context.ComposeContent))
        {
            var composePath = Path.Combine(context.StagingDirectory!, DeployPackageLayout.ComposeFile);
            artifacts["compose.yml"] = composePath;
        }

        return (PublishingStageState.Passed, "Compose artifacts generated.", artifacts, []);
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunConfigurationGenerationAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        context.Manifest = manifestGenerator.Generate(context);
        context.Health = healthDefinitionGenerator.Generate(context);
        await configTemplateGenerator.GenerateAsync(context, cancellationToken);
        await envTemplateGenerator.GenerateAsync(context, cancellationToken);

        var neutralityErrors = PublishingEnvironmentNeutralityValidator.Validate(context);
        if (neutralityErrors.Count > 0)
        {
            return (PublishingStageState.Failed, string.Join("; ", neutralityErrors), new Dictionary<string, string>(), []);
        }

        return (PublishingStageState.Passed, "Configuration templates generated.", new Dictionary<string, string>(), []);
    }

    private Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunContractGenerationAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        context.DeploymentContracts = ServiceContractGenerator.Generate(context);

        var validation = PublishingContractValidationService.Validate(context);
        if (!validation.Passed)
        {
            return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                (PublishingStageState.Failed, string.Join("; ", validation.Errors), new Dictionary<string, string>(), []));
        }

        return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
            (PublishingStageState.Passed, "Deployment contracts generated.", new Dictionary<string, string>(), []));
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunPackageGenerationAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        var version = context.Request.Version ?? PublishingVersionPolicy.DefaultPackageVersion;
        var identity = buildIdentityService.FinalizeInputFingerprint(context);
        context.Metadata = new DeployPackageMetadata
        {
            Publisher = settings.CurrentValue.PublisherName,
            PublishingEngineVersion = EngineVersion,
            PackageVersion = version,
            PackageType = DeployPackageLayout.Extension,
            CreatedUtc = DateTimeOffset.UtcNow,
            SourceRevision = identity.SourceRevision,
            PublishingInputFingerprint = identity.PublishingInputFingerprint,
            DockerImageIdentity = identity.DockerImageIdentity,
            FingerprintPolicyVersion = identity.FingerprintPolicyVersion
        };

        var stage = await deployPackageBuilder.StageAsync(context, cancellationToken);
        if (!stage.IsSuccess)
        {
            return (PublishingStageState.Failed, stage.Message, new Dictionary<string, string>(), []);
        }

        var stagingErrors = PublishingStagingIntegrityValidator.Validate(context);
        if (stagingErrors.Count > 0)
        {
            return (PublishingStageState.Failed, string.Join("; ", stagingErrors), new Dictionary<string, string>(), []);
        }

        var artifactFingerprints = buildIdentityService.ComputeStagingArtifactFingerprints(context);
        PublishingBuildManifestWriter.Write(context, artifactFingerprints);

        if (context.DeploymentContracts is not null && !string.IsNullOrWhiteSpace(context.ComposeContent))
        {
            var mountValidation = ComposeMountValidator.Validate(
                context.ComposeContent,
                context.DeploymentContracts.Volume,
                context.DeploymentContracts.Filesystem);

            if (!mountValidation.Passed)
            {
                return (PublishingStageState.Failed,
                    $"Compose infrastructure validation failed: {string.Join("; ", mountValidation.Errors)}",
                    new Dictionary<string, string>(),
                    []);
            }
        }

        await deployPackageBuilder.CreateInterimArchiveAsync(context, cancellationToken);
        if (string.IsNullOrWhiteSpace(context.InterimPackagePath))
        {
            return (PublishingStageState.Failed, "Interim package path was not created.", new Dictionary<string, string>(), []);
        }

        return (PublishingStageState.Passed, "Interim package generated.", new Dictionary<string, string>
        {
            ["interim-package"] = context.InterimPackagePath
        }, []);
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunPackageVerificationAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        var report = await packageVerifier.VerifyAsync(
            context.InterimPackagePath!,
            expectCertificate: false,
            expectDeterministicFingerprint: true,
            cancellationToken);
        context.PackageVerificationReport = report;
        context.PackageVerification = report.IsValid
            ? DeployPackageVerificationResult.Success(report.VerifiedFiles, report.ChecksumCount, report.HasDeploymentCertificate)
            : DeployPackageVerificationResult.Failure(report.Errors);

        if (context.BuildIdentity is not null && !string.IsNullOrWhiteSpace(report.DeterministicFingerprint))
        {
            context.BuildIdentity.PackageFingerprint = report.DeterministicFingerprint;
        }

        if (!report.IsValid)
        {
            return (PublishingStageState.Failed, string.Join("; ", report.Errors), new Dictionary<string, string>(), []);
        }

        var serviceId = context.Manifest?.Service ?? context.Request.ServiceId ?? "service";
        var golden = await packageVerifier.CompareToGoldenAsync(context.InterimPackagePath!, serviceId, cancellationToken);
        if (golden.RequiresApproval)
        {
            return (PublishingStageState.Failed, string.Join("; ", golden.Differences), new Dictionary<string, string>(), []);
        }

        return (PublishingStageState.Passed, $"Package verification passed ({report.ChecksumCount} checksums).", new Dictionary<string, string>(), []);
    }

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunDrvAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        if (context.Request.DeferDeploymentReadinessValidation)
        {
            return (PublishingStageState.Skipped, "Deployment readiness validation deferred.", new Dictionary<string, string>(), []);
        }

        if (!settings.CurrentValue.EnableDeploymentReadinessValidation)
        {
            var validation = await deployPackageValidator.ValidateAsync(context.InterimPackagePath!, cancellationToken);
            context.Warnings.AddRange(validation.Warnings);
            if (!validation.IsValid)
            {
                return (PublishingStageState.Failed, string.Join("; ", validation.Errors), new Dictionary<string, string>(), []);
            }

            context.ReadinessReport = DeploymentReadinessReport.Ready([], TimeSpan.Zero);
            return (PublishingStageState.Passed, "Deployment readiness validation disabled; package validator passed.", new Dictionary<string, string>(), context.Warnings);
        }

        var contents = await deployPackageReader.ReadAsync(context.InterimPackagePath!, cancellationToken);
        if (contents is null)
        {
            return (PublishingStageState.Failed, "Unable to read deployment package for readiness validation.", new Dictionary<string, string>(), []);
        }

        var validationDirectory = Path.Combine(
            settings.CurrentValue.DeploymentValidationFolder,
            Path.GetFileNameWithoutExtension(context.InterimPackagePath!),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(validationDirectory);

        var drvContext = new DeploymentValidationContext
        {
            PackagePath = context.InterimPackagePath!,
            PackageContents = contents,
            DeploymentContracts = contents.DeploymentContracts,
            WorkingDirectory = validationDirectory,
            ValidationConfigRoot = settings.CurrentValue.ValidationConfigRoot,
            ValidationProfile = settings.CurrentValue.ValidationProfile,
            ExpectedDockerNetwork = settings.CurrentValue.DefaultNetworkName,
            ContainerPort = context.Discovery?.ContainerPort ?? RuntimeAgentConventions.DefaultPort,
            DockerExecutable = settings.CurrentValue.DockerExecutable,
            TimeoutSeconds = settings.CurrentValue.DeploymentValidationTimeoutSeconds,
            ExecutionScope = ValidationExecutionScope.Publishing,
            ReportProgress = (stageName, status, message, percent) =>
                context.Report($"DRV: {stageName}", status, message, percent)
        };

        var report = await deploymentValidator.ValidateAsync(drvContext, cancellationToken);
        context.ReadinessReport = report;
        context.Warnings.AddRange(report.Warnings);
        if (!string.IsNullOrWhiteSpace(report.LogFilePath))
        {
            context.GeneratedFiles.Add(report.LogFilePath);
        }

        return report.IsReady
            ? (PublishingStageState.Passed, "Publishing readiness validation passed.", new Dictionary<string, string>(), report.Warnings)
            : (PublishingStageState.Failed, string.Join("; ", report.Errors), new Dictionary<string, string>(), report.Warnings);
    }

    private Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunReleaseLibraryAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (context.ExecutionOptions.SkipReleaseLibrary)
        {
            return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                (PublishingStageState.Skipped, "Release library stage skipped.", new Dictionary<string, string>(), []));
        }

        // The final certified package does not exist yet at this point in the
        // canonical order (DeploymentCertification, the next stage, is what
        // produces it) — see the M1.5 report §3 for why. This stage instead runs
        // the Release Library's D10 pre-flight lifecycle validation as early and
        // as cheaply as possible, using the fingerprint already computed during
        // Package Verification, so a duplicate-artifact or corrupted-index
        // problem fails the publish before certification runs at all.
        var serviceId = context.Manifest?.Service ?? context.Request.ServiceId;
        var version = context.Manifest?.Version ?? context.Request.Version;
        var fingerprint = context.PackageVerificationReport?.DeterministicFingerprint;

        if (string.IsNullOrWhiteSpace(serviceId) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(fingerprint))
        {
            return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                (PublishingStageState.Passed, "Release Library pre-flight skipped (no fingerprint yet).", new Dictionary<string, string>(), []));
        }

        var releaseLibraryRoot = ResolveReleaseLibraryRoot(context);

        try
        {
            var artifactId = ReleaseLibraryService.BuildArtifactId(serviceId, version);
            var existingSameId = releaseLibraryService.FindByArtifactId(releaseLibraryRoot, artifactId);
            if (existingSameId is not null && !string.Equals(existingSameId.PublishingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                    (PublishingStageState.Failed,
                        $"Release Library already contains artifact '{artifactId}' with a different publishing fingerprint "
                        + $"(existing='{existingSameId.PublishingFingerprint}', new='{fingerprint}'). Publish a new version instead of reusing '{version}'.",
                        new Dictionary<string, string>(), []));
            }

            var fingerprintCollision = releaseLibraryService.FindByFingerprint(releaseLibraryRoot, fingerprint);
            if (fingerprintCollision is not null && !string.Equals(fingerprintCollision.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                    (PublishingStageState.Failed,
                        $"Publishing fingerprint '{fingerprint}' is already registered under a different service "
                        + $"('{fingerprintCollision.ServiceId}'). This indicates a fingerprint collision or index corruption.",
                        new Dictionary<string, string>(), []));
            }
        }
        catch (InvalidOperationException ex)
        {
            // Thrown by ReleaseIndexStore.Load for a corrupted or unparseable index (D10).
            return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
                (PublishingStageState.Failed, $"Release Library index is inconsistent: {ex.Message}", new Dictionary<string, string>(), []));
        }

        return Task.FromResult<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)>(
            (PublishingStageState.Passed, "Release Library pre-flight validation passed.", new Dictionary<string, string>(), []));
    }

    private string ResolveReleaseLibraryRoot(PublishingContext context)
        => context.Request.OutputDirectory ?? settings.CurrentValue.OutputFolder;

    private async Task<(PublishingStageState, string, Dictionary<string, string>, IReadOnlyList<string>)> RunDeploymentCertificationAsync(
        PublishingContext context,
        CancellationToken cancellationToken)
    {
        if (context.ExecutionOptions.SkipDeploymentCertification || context.Request.DeferDeploymentReadinessValidation)
        {
            return (PublishingStageState.Skipped, "Deployment certification deferred.", new Dictionary<string, string>(), []);
        }

        var finalize = await deployPackageBuilder.FinalizeAsync(context, context.ReadinessReport ?? DeploymentReadinessReport.Ready([], TimeSpan.Zero), cancellationToken);
        if (!finalize.IsSuccess)
        {
            return (PublishingStageState.Failed, finalize.Message, new Dictionary<string, string>(), []);
        }

        var report = await packageVerifier.VerifyAsync(
            context.PackagePath!,
            expectCertificate: true,
            expectDeterministicFingerprint: true,
            cancellationToken);
        context.PackageVerificationReport = report;
        if (!report.IsValid)
        {
            return (PublishingStageState.Failed, string.Join("; ", report.Errors), new Dictionary<string, string>(), []);
        }

        var serviceId = context.Manifest?.Service ?? context.Request.ServiceId ?? "service";
        if (!string.IsNullOrWhiteSpace(report.DeterministicFingerprint))
        {
            await goldenPackageRepository.StoreGoldenAsync(serviceId, context.PackagePath!, report.DeterministicFingerprint, cancellationToken);
        }

        if (!context.ExecutionOptions.SkipReleaseLibrary && context.Manifest is not null && context.BuildIdentity is not null
            && !string.IsNullOrWhiteSpace(report.DeterministicFingerprint))
        {
            try
            {
                var releaseLibraryRoot = ResolveReleaseLibraryRoot(context);
                var registration = new ReleaseArtifactRegistration
                {
                    ServiceId = serviceId,
                    ServiceDisplayName = serviceId,
                    Version = context.Manifest.Version,
                    PackagePath = context.PackagePath!,
                    SourceRevision = context.BuildIdentity.SourceRevision,
                    BuildUtc = context.BuildIdentity.BuildUtc,
                    PublishingFingerprint = report.DeterministicFingerprint,
                    ContractVersion = Aovias.Deployment.Contracts.Building.ServiceContractBuilder.ContractVersion,
                    PublishingEngineVersion = context.BuildIdentity.PublishingEngineVersion,
                    SupportedRuntimeVersion = context.BuildIdentity.TargetFramework,
                    FingerprintPolicyVersion = context.BuildIdentity.FingerprintPolicyVersion,
                    ReleaseLibraryCategory = context.DeploymentMetadata?.ReleaseLibraryCategory ?? "Services",
                    Dependencies = context.Manifest.Dependencies
                };

                // Publishing owns Building -> Published (D2); this same stage,
                // as the pipeline's own certification step, owns Published/Verified
                // -> Certified — see IReleaseLibraryService for the ownership
                // contract of each transition.
                var record = await releaseLibraryService.RegisterPublishedAsync(releaseLibraryRoot, registration, cancellationToken);
                if (record.LifecycleState < ArtifactLifecycleState.Certified)
                {
                    await releaseLibraryService.AdvanceToCertifiedAsync(releaseLibraryRoot, record.ArtifactId, cancellationToken);
                }
            }
            catch (InvalidOperationException ex)
            {
                return (PublishingStageState.Failed, $"Release Library registration failed: {ex.Message}", new Dictionary<string, string>(), []);
            }
        }

        return (PublishingStageState.Passed, "Deployment certified.", new Dictionary<string, string>
        {
            ["package"] = context.PackagePath!
        }, []);
    }

    private static void AppendOutput(PublishingContext context, ProcessExecutionResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            context.BuildOutput.Add(result.StandardOutput);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            context.BuildOutput.Add(result.StandardError);
        }
    }

    private static string FormatDockerUnavailableMessage(ProcessExecutionResult inspect)
        => string.IsNullOrWhiteSpace(inspect.StandardError)
            ? "Docker engine is unavailable."
            : inspect.StandardError;
}
