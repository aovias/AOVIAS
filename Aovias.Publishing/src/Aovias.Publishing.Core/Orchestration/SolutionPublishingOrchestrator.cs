using System.Collections.Concurrent;
using System.Diagnostics;
using Aovias.Configuration.Settings;
using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Orchestration;

/// <summary>
/// Publishes every deployable project in the platform solution with failure isolation and parallel stages.
/// </summary>
public sealed class SolutionPublishingOrchestrator : ISolutionPublishingOrchestrator
{
    private readonly IDeployableProjectDiscoverer discoverer;
    private readonly IPublishingPipeline publishingPipeline;
    private readonly ISolutionPublishingPreflight preflight;
    private readonly ISolutionPackageInspector packageInspector;
    private readonly ValidationWorkspaceManager validationWorkspaceManager;
    private readonly IPlatformDeploymentSimulationService platformSimulationService;
    private readonly IReleaseLibraryService releaseLibraryService;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<SolutionPublishingOrchestrator> logger;
    private readonly SolutionBundleBuilder bundleBuilder = new();

    public SolutionPublishingOrchestrator(
        IDeployableProjectDiscoverer discoverer,
        IPublishingPipeline publishingPipeline,
        ISolutionPublishingPreflight preflight,
        ISolutionPackageInspector packageInspector,
        ValidationWorkspaceManager validationWorkspaceManager,
        IPlatformDeploymentSimulationService platformSimulationService,
        IReleaseLibraryService releaseLibraryService,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<SolutionPublishingOrchestrator> logger)
    {
        this.discoverer = discoverer;
        this.publishingPipeline = publishingPipeline;
        this.preflight = preflight;
        this.packageInspector = packageInspector;
        this.validationWorkspaceManager = validationWorkspaceManager;
        this.platformSimulationService = platformSimulationService;
        this.releaseLibraryService = releaseLibraryService;
        this.settings = settings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<SolutionPublishingResult> PublishSolutionAsync(
        SolutionPublishingRequest request,
        IProgress<SolutionPublishingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var outputDirectory = request.OutputDirectory ?? settings.CurrentValue.OutputFolder;
        var logDirectory = Path.Combine(Path.GetDirectoryName(outputDirectory) ?? outputDirectory, "Publishing");

        SolutionPublishingPreflightResult? preflightResult = null;
        if (!request.SkipPreflight)
        {
            Report(progress, "Preflight", "Running publish preflight verification...", 0, 0, null);
            preflightResult = await preflight.RunAsync(
                new SolutionPublishingPreflightRequest
                {
                    PlatformRoot = request.PlatformRoot,
                    Profile = request.Profile,
                    Version = request.Version,
                    OutputDirectory = outputDirectory
                },
                new Progress<SolutionPublishingPreflightProgress>(update =>
                    Report(progress, "Preflight", $"[{update.Category}] {update.Message}", 0, 0, null)),
                cancellationToken);

            if (!preflightResult.IsReady)
            {
                var blockedMessage = $"BLOCKED: {string.Join("; ", preflightResult.BlockingReasons)}";
                return new SolutionPublishingResult
                {
                    IsSuccess = false,
                    Message = blockedMessage,
                    PreflightResult = preflightResult,
                    PublishingLogDirectory = logDirectory,
                    TotalDuration = stopwatch.Elapsed
                };
            }

            Report(progress, "Preflight", "READY TO PUBLISH", 0, 0, null);
        }

        Directory.CreateDirectory(outputDirectory);

        using var logWriter = new SolutionPublishingLogWriter(logDirectory);
        logWriter.WriteSummary($"Starting solution publish. Profile={request.Profile}, Version={request.Version}");

        Report(progress, "Analyze Solution", "Discovering deployable projects...", 0, 0, null);
        var projects = await discoverer.DiscoverAsync(request.PlatformRoot, cancellationToken);
        if (projects.Count == 0)
        {
            logWriter.WriteSummary("No deployable projects were discovered.");
            return Failed("No deployable projects were discovered.", stopwatch.Elapsed, logDirectory);
        }

        logWriter.WriteSummary($"Discovered {projects.Count} deployable project(s).");
        Report(progress, "Discover Projects", $"Discovered {projects.Count} project(s).", 0, projects.Count, null);

        var releaseLibraryRoot = outputDirectory;
        var results = new ConcurrentBag<SolutionProjectPublishResult>();
        var completed = 0;
        var maxConcurrency = Math.Max(1, request.MaxConcurrency ?? Environment.ProcessorCount);
        var sequentialDrv = settings.CurrentValue.SequentialDeploymentValidation;
        var resourceMonitor = new DrvResourceMonitor(outputDirectory);

        Report(progress, "Publish", sequentialDrv
            ? "Building and packaging projects in parallel..."
            : "Publishing projects...", 0, projects.Count, null);

        await Parallel.ForEachAsync(
            projects,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            },
            async (project, token) =>
            {
                var result = await PublishProjectAsync(
                    project,
                    request,
                    outputDirectory,
                    releaseLibraryRoot,
                    logWriter,
                    request.OrganizeReleaseLibrary,
                    sequentialDrv,
                    progress,
                    Interlocked.Increment(ref completed),
                    projects.Count,
                    token);
                results.Add(result);
                resourceMonitor.Sample();
            });

        var orderedResults = results
            .OrderBy(result => result.Project.StartupOrder)
            .ThenBy(result => result.Project.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sequentialDrv)
        {
            Report(progress, "Deployment Validation", "Running sequential deployment readiness validation...", completed, projects.Count, null);
            var drvCompleted = 0;
            for (var index = 0; index < orderedResults.Count; index++)
            {
                var result = orderedResults[index];
                if (result.PendingValidation is null || result.IsDeploymentReady)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var validated = await ValidateProjectSequentiallyAsync(
                    result,
                    progress,
                    drvCompleted,
                    projects.Count,
                    resourceMonitor,
                    logWriter,
                    cancellationToken);
                orderedResults[index] = validated;
                drvCompleted++;
                var freedBytes = await validationWorkspaceManager.ResetAfterValidationAsync(cancellationToken);
                resourceMonitor.RecordFreed(freedBytes);
                resourceMonitor.Sample();
            }

            await validationWorkspaceManager.DisposeSessionAsync(cancellationToken);
            resourceMonitor.Sample();
        }

        if (request.OrganizeReleaseLibrary)
        {
            orderedResults = await SyncReleaseLibraryAsync(releaseLibraryRoot, orderedResults, logWriter, cancellationToken);
        }

        var succeeded = orderedResults.Count(result => result.IsSuccess);
        var deploymentReady = orderedResults.Count(result => result.IsDeploymentReady);
        var failed = orderedResults.Count - succeeded;

        foreach (var result in orderedResults)
        {
            var line = result.IsDeploymentReady
                ? $"PASS {result.Project.ServiceId} ({result.Duration.TotalSeconds:F1}s) -> {result.PackagePath}"
                : $"FAIL {result.Project.ServiceId}: {result.ErrorMessage ?? "Unknown error"}";
            logWriter.WriteSummary(line);
        }

        string? manifestPath = null;
        string? bundlePath = null;
        if (succeeded > 0)
        {
            Report(progress, "Generate Packages", "Writing solution deployment manifest...", completed, projects.Count, null);
            var bundleResults = FilterResultsForProfile(orderedResults, request.Profile);
            var manifest = bundleBuilder.CreateSolutionDeploymentManifest(bundleResults, request.Profile, request.Version);
            manifestPath = bundleBuilder.WriteSolutionDeploymentManifest(manifest, outputDirectory);
            logWriter.WriteSummary($"Wrote {manifestPath}");

            if (request.GenerateDemoBundle)
            {
                Report(progress, "Generate Packages", "Creating DemoDeployment.zip...", completed, projects.Count, null);
                bundlePath = bundleBuilder.CreateDemoDeploymentBundle(manifest, bundleResults, outputDirectory);
                logWriter.WriteSummary($"Wrote {bundlePath}");
            }
        }

        stopwatch.Stop();
        logWriter.WriteSummary($"Completed in {stopwatch.Elapsed.TotalSeconds:F1}s. Success={succeeded}, DeploymentReady={deploymentReady}, Failed={failed}.");

        SolutionPackageInspectionResult? inspectionResult = null;
        var platformSimulationPassed = true;
        string? platformSimulationMessage = null;

        if (succeeded > 0)
        {
            Report(progress, "Simulate Deployment", "Running continuous deployment simulation...", projects.Count, projects.Count, null);
            var simulation = await platformSimulationService.SimulateAsync(releaseLibraryRoot, cancellationToken);
            platformSimulationPassed = simulation.Passed;
            platformSimulationMessage = simulation.Message;
            logWriter.WriteSummary(platformSimulationPassed
                ? $"Platform deployment simulation passed: {simulation.SimulationId}"
                : $"Platform deployment simulation failed: {simulation.Message}");
        }

        if (request.InspectPackagesAfterPublish && succeeded > 0)
        {
            Report(progress, "Inspect Packages", "Verifying release library packages...", projects.Count, projects.Count, null);
            inspectionResult = await packageInspector.InspectReleaseLibraryAsync(releaseLibraryRoot, request.Profile, cancellationToken);
            logWriter.WriteSummary(inspectionResult.IsValid
                ? $"Package inspection passed: {inspectionResult.Summary}"
                : $"Package inspection failed: {string.Join("; ", inspectionResult.Errors)}");
        }

        Report(progress, "Complete", $"Solution publish finished. {deploymentReady}/{projects.Count} deployment ready.", projects.Count, projects.Count, null);

        return new SolutionPublishingResult
        {
            IsSuccess = failed == 0 && platformSimulationPassed && (inspectionResult?.IsValid ?? true),
            Message = failed == 0
                ? !platformSimulationPassed
                    ? $"Publish completed but deployment simulation failed: {platformSimulationMessage}"
                    : inspectionResult is { IsValid: false }
                    ? $"Publish completed but package inspection failed: {inspectionResult.Summary}"
                    : $"All {projects.Count} project(s) published successfully. {deploymentReady} deployment ready."
                : $"{failed} of {projects.Count} project(s) failed. {deploymentReady} deployment ready.",
            ProjectResults = orderedResults,
            SolutionDeploymentManifestPath = manifestPath,
            DemoDeploymentBundlePath = bundlePath,
            SummaryLogPath = logWriter.SummaryLogPath,
            PublishingLogDirectory = logDirectory,
            SucceededCount = succeeded,
            FailedCount = failed,
            DeploymentReadyCount = deploymentReady,
            TotalDuration = stopwatch.Elapsed,
            PreflightResult = preflightResult,
            PackageInspectionResult = inspectionResult,
            ResourceSnapshot = MapResourceSnapshot(resourceMonitor.CreateSnapshot()),
            PlatformSimulationPassed = platformSimulationPassed,
            PlatformSimulationMessage = platformSimulationMessage
        };
    }

    private async Task<SolutionProjectPublishResult> PublishProjectAsync(
        DeployableProject project,
        SolutionPublishingRequest request,
        string outputDirectory,
        string releaseLibraryRoot,
        SolutionPublishingLogWriter logWriter,
        bool organizeReleaseLibrary,
        bool deferDeploymentValidation,
        IProgress<SolutionPublishingProgress>? progress,
        int completedProjects,
        int totalProjects,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = CreateStatus(project, "Starting", 0);
        Report(progress, "Publish", $"Publishing {project.Name}...", completedProjects, totalProjects, status);
        logWriter.WriteProject(project.ServiceId, $"Starting publish for {project.Name} ({project.DiscoveryPath})");

        try
        {
            var projectProgress = new Progress<PublishingProgress>(update =>
            {
                var mapped = MapProjectStatus(project, update, stopwatch.Elapsed);
                Report(progress, "Publish", update.Message, completedProjects, totalProjects, mapped);
                logWriter.WriteProject(project.ServiceId, $"{update.StepName}: {update.Message}");
            });

            var resolvedVersion = request.ServiceVersionOverrides is not null
                && request.ServiceVersionOverrides.TryGetValue(project.ServiceId, out var overrideVersion)
                && !string.IsNullOrWhiteSpace(overrideVersion)
                    ? overrideVersion
                    : request.Version;

            var publishResult = await publishingPipeline.ExecuteAsync(
                new PublishingRequest
                {
                    ProjectPath = project.ProjectPath,
                    ServiceId = project.ServiceId,
                    Version = resolvedVersion,
                    OutputDirectory = outputDirectory,
                    Profile = request.Profile,
                    Publisher = request.Publisher,
                    DeferDeploymentReadinessValidation = deferDeploymentValidation
                        && settings.CurrentValue.EnableDeploymentReadinessValidation,
                    DeploymentValidationMode = request.DeploymentValidationMode
                        ?? settings.CurrentValue.DeploymentValidationMode
                },
                projectProgress,
                cancellationToken);

            stopwatch.Stop();
            var finalStatus = BuildFinalStatus(project, publishResult, stopwatch.Elapsed);
            Report(progress, "Publish", finalStatus.DeploymentReady ? "Deployment Ready" : publishResult.Message, completedProjects, totalProjects, finalStatus);

            string? libraryPath = null;
            if (publishResult.IsSuccess
                && publishResult.IsDeploymentCertified
                && !string.IsNullOrWhiteSpace(publishResult.PackagePath)
                && organizeReleaseLibrary)
            {
                libraryPath = await releaseLibraryService.RegisterOrganizedCopyAsync(releaseLibraryRoot, project, publishResult.PackagePath, cancellationToken);
                logWriter.WriteProject(project.ServiceId, $"Registered in release library: {libraryPath}");
            }

            if (publishResult.IsDeploymentReady)
            {
                logWriter.WriteProject(project.ServiceId, $"Deployment Ready: {publishResult.PackagePath}");
            }
            else if (publishResult.PendingValidation is not null)
            {
                logWriter.WriteProject(project.ServiceId, $"Package ready, DRV pending: {publishResult.PackagePath}");
            }
            else
            {
                logWriter.WriteProject(project.ServiceId, $"Failed: {publishResult.Message}");
            }

            return new SolutionProjectPublishResult
            {
                Project = project,
                IsSuccess = publishResult.IsSuccess,
                IsDeploymentReady = publishResult.IsDeploymentReady,
                PackagePath = publishResult.PackagePath,
                ReleaseLibraryPath = libraryPath,
                LogPath = logWriter.GetProjectLogPath(project.ServiceId),
                ErrorMessage = publishResult.IsSuccess ? null : publishResult.Message,
                Duration = stopwatch.Elapsed,
                PublishingResult = publishResult,
                PendingValidation = publishResult.PendingValidation
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Publishing failed for {ServiceId}", project.ServiceId);
            logWriter.WriteProject(project.ServiceId, $"Exception: {ex.Message}");

            var failedStatus = new SolutionPublishProjectStatus
            {
                ProjectName = project.Name,
                ServiceId = project.ServiceId,
                CurrentStep = "Failed",
                Failed = true,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
            Report(progress, "Publish", ex.Message, completedProjects, totalProjects, failedStatus);

            return new SolutionProjectPublishResult
            {
                Project = project,
                IsSuccess = false,
                IsDeploymentReady = false,
                LogPath = logWriter.GetProjectLogPath(project.ServiceId),
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<SolutionProjectPublishResult> ValidateProjectSequentiallyAsync(
        SolutionProjectPublishResult result,
        IProgress<SolutionPublishingProgress>? progress,
        int completedDrv,
        int totalProjects,
        DrvResourceMonitor resourceMonitor,
        SolutionPublishingLogWriter logWriter,
        CancellationToken cancellationToken)
    {
        var pending = result.PendingValidation!;
        var project = result.Project;
        var stopwatch = Stopwatch.StartNew();

        Report(progress, "Deployment Validation", $"Validating {project.ServiceId}...", completedDrv, totalProjects, new SolutionPublishProjectStatus
        {
            ProjectName = project.Name,
            ServiceId = project.ServiceId,
            CurrentStep = "DRV",
            PercentComplete = 0
        });

        var projectProgress = new Progress<PublishingProgress>(update =>
        {
            Report(progress, "Deployment Validation", update.Message, completedDrv, totalProjects, new SolutionPublishProjectStatus
            {
                ProjectName = project.Name,
                ServiceId = project.ServiceId,
                CurrentStep = update.StepName,
                ValidationSucceeded = update.StepName.Contains("DRV", StringComparison.OrdinalIgnoreCase),
                PercentComplete = update.PercentComplete
            });
        });

        var publishResult = await publishingPipeline.CompleteDeferredDeploymentReadinessAsync(
            pending,
            projectProgress,
            cancellationToken,
            resourceMonitor.Sample,
            resourceMonitor.RecordFreed);

        stopwatch.Stop();
        resourceMonitor.Sample();

        if (publishResult.IsDeploymentReady)
        {
            logWriter.WriteProject(project.ServiceId, $"Deployment Ready: {publishResult.PackagePath}");
        }
        else
        {
            logWriter.WriteProject(project.ServiceId, $"DRV failed: {publishResult.Message}");
        }

        return new SolutionProjectPublishResult
        {
            Project = project,
            IsSuccess = publishResult.IsSuccess,
            IsDeploymentReady = publishResult.IsDeploymentReady,
            PackagePath = publishResult.PackagePath ?? result.PackagePath,
            ReleaseLibraryPath = result.ReleaseLibraryPath,
            LogPath = result.LogPath,
            ErrorMessage = publishResult.IsSuccess ? null : publishResult.Message,
            Duration = result.Duration + stopwatch.Elapsed,
            PublishingResult = publishResult
        };
    }

    private async Task<List<SolutionProjectPublishResult>> SyncReleaseLibraryAsync(
        string releaseLibraryRoot,
        IReadOnlyList<SolutionProjectPublishResult> results,
        SolutionPublishingLogWriter logWriter,
        CancellationToken cancellationToken)
    {
        var synced = new List<SolutionProjectPublishResult>(results.Count);
        foreach (var result in results)
        {
            if (!result.IsDeploymentReady || string.IsNullOrWhiteSpace(result.PackagePath))
            {
                synced.Add(result);
                continue;
            }

            var libraryPath = await releaseLibraryService.RegisterOrganizedCopyAsync(releaseLibraryRoot, result.Project, result.PackagePath, cancellationToken);
            if (!string.Equals(result.ReleaseLibraryPath, libraryPath, StringComparison.OrdinalIgnoreCase))
            {
                logWriter.WriteProject(result.Project.ServiceId, $"Synced release library: {libraryPath}");
            }

            synced.Add(new SolutionProjectPublishResult
            {
                Project = result.Project,
                IsSuccess = result.IsSuccess,
                IsDeploymentReady = result.IsDeploymentReady,
                PackagePath = result.PackagePath,
                ReleaseLibraryPath = libraryPath,
                LogPath = result.LogPath,
                PendingValidation = result.PendingValidation,
                ErrorMessage = result.ErrorMessage,
                Duration = result.Duration,
                PublishingResult = result.PublishingResult
            });
        }

        return synced;
    }

    private static PublishingResourceSnapshot MapResourceSnapshot(DrvResourceSnapshot snapshot)
        => new()
        {
            CurrentAvailableBytes = snapshot.CurrentAvailableBytes,
            PeakConsumedBytes = snapshot.PeakConsumedBytes,
            FreedBytes = snapshot.FreedBytes,
            RemainingBytes = snapshot.RemainingBytes
        };

    private static SolutionPublishProjectStatus BuildFinalStatus(
        DeployableProject project,
        PublishingResult result,
        TimeSpan duration)
    {
        var buildSucceeded = result.IsSuccess || result.StageTimings.Any(stage => stage.StageName.Contains("Build", StringComparison.OrdinalIgnoreCase));
        var dockerSucceeded = result.IsSuccess || result.GeneratedFiles.Any(file => file.EndsWith(".tar", StringComparison.OrdinalIgnoreCase));
        var packageSucceeded = !string.IsNullOrWhiteSpace(result.PackagePath);
        var validationSucceeded = result.ReadinessReport?.IsReady == true;

        return new SolutionPublishProjectStatus
        {
            ProjectName = project.Name,
            ServiceId = project.ServiceId,
            CurrentStep = result.IsDeploymentReady ? "Deployment Ready" : result.Message,
            BuildSucceeded = buildSucceeded,
            DockerSucceeded = dockerSucceeded,
            PackageSucceeded = packageSucceeded,
            ValidationSucceeded = validationSucceeded,
            DeploymentReady = result.IsDeploymentReady,
            Failed = !result.IsSuccess,
            PercentComplete = result.IsDeploymentReady ? 100 : result.IsSuccess ? 90 : 0,
            Duration = duration,
            ErrorMessage = result.IsSuccess ? null : result.Message,
            PackagePath = result.PackagePath
        };
    }

    /// <summary>
    /// Maps live progress updates to solution UI status via step-name heuristics.
    /// Prefer <see cref="BuildFinalStatus"/> for terminal state; stage-ID mapping deferred to M1.4.
    /// </summary>
    private static SolutionPublishProjectStatus MapProjectStatus(
        DeployableProject project,
        PublishingProgress update,
        TimeSpan duration)
    {
        var step = update.StepName;
        return new SolutionPublishProjectStatus
        {
            ProjectName = project.Name,
            ServiceId = project.ServiceId,
            CurrentStep = step,
            BuildSucceeded = ContainsAny(step, "Build", "Publish"),
            DockerSucceeded = ContainsAny(step, "Docker"),
            PackageSucceeded = ContainsAny(step, "Package", "Finalize", "Deployment Ready"),
            ValidationSucceeded = ContainsAny(step, "Validation", "Deployment Readiness", "Deployment Ready"),
            DeploymentReady = ContainsAny(step, "Deployment Ready"),
            Failed = update.Message.Contains("fail", StringComparison.OrdinalIgnoreCase),
            PercentComplete = update.PercentComplete,
            Duration = duration,
            ErrorMessage = update.Message.Contains("fail", StringComparison.OrdinalIgnoreCase) ? update.Message : null
        };
    }

    private static SolutionPublishProjectStatus CreateStatus(DeployableProject project, string step, int percent)
        => new()
        {
            ProjectName = project.Name,
            ServiceId = project.ServiceId,
            CurrentStep = step,
            PercentComplete = percent,
            Duration = TimeSpan.Zero
        };

    private static bool ContainsAny(string value, params string[] tokens)
        => tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static void Report(
        IProgress<SolutionPublishingProgress>? progress,
        string phase,
        string message,
        int completedProjects,
        int totalProjects,
        SolutionPublishProjectStatus? projectStatus)
    {
        progress?.Report(new SolutionPublishingProgress
        {
            Phase = phase,
            Message = message,
            CompletedProjects = completedProjects,
            TotalProjects = totalProjects,
            ProjectStatus = projectStatus
        });
    }

    private static IReadOnlyList<SolutionProjectPublishResult> FilterResultsForProfile(
        IReadOnlyList<SolutionProjectPublishResult> results,
        string profile)
    {
        if (!profile.Equals("demo", StringComparison.OrdinalIgnoreCase))
        {
            return results;
        }

        return results
            .Where(result => SolutionMembershipResolver.IsDemoBundleMember(result.Project))
            .ToList();
    }

    private static SolutionPublishingResult Failed(string message, TimeSpan duration, string logDirectory)
        => new()
        {
            IsSuccess = false,
            Message = message,
            PublishingLogDirectory = logDirectory,
            SummaryLogPath = Path.Combine(logDirectory, "Summary.log"),
            TotalDuration = duration
        };
}
