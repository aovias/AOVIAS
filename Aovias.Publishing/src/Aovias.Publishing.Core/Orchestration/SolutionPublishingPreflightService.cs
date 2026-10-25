using System.Diagnostics;
using System.Text.RegularExpressions;
using Aovias.Configuration.Deployment;
using Aovias.Configuration.Layout;
using Aovias.Configuration.Settings;
using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Orchestration;

/// <summary>
/// Runs comprehensive preflight checks before solution publishing.
/// </summary>
public sealed class SolutionPublishingPreflightService : ISolutionPublishingPreflight
{
    private const long BytesPerServiceImage = 600L * 1024 * 1024;
    private const long BytesPerPackage = 150L * 1024 * 1024;
    private const long BytesValidationWorkspace = 2L * 1024 * 1024 * 1024;
    private const long SafetyMarginBytes = 2L * 1024 * 1024 * 1024;

    private static readonly Regex WarningRegex = new(@"\swarning\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IDeployableProjectDiscoverer discoverer;
    private readonly IProcessRunner processRunner;
    private readonly IOptionsMonitor<PublishingSettings> settings;
    private readonly ILogger<SolutionPublishingPreflightService> logger;

    public SolutionPublishingPreflightService(
        IDeployableProjectDiscoverer discoverer,
        IProcessRunner processRunner,
        IOptionsMonitor<PublishingSettings> settings,
        ILogger<SolutionPublishingPreflightService> logger)
    {
        this.discoverer = discoverer;
        this.processRunner = processRunner;
        this.settings = settings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<SolutionPublishingPreflightResult> RunAsync(
        SolutionPublishingPreflightRequest request,
        IProgress<SolutionPublishingPreflightProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var publishingSettings = settings.CurrentValue;
        var outputDirectory = request.OutputDirectory ?? publishingSettings.OutputFolder;
        var blocking = new List<string>();
        var warnings = new List<string>();
        var checks = new List<SolutionPreflightCheckResult>();
        var projectWarnings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Report(progress, "Start", "Starting publish preflight verification...");

        var projects = await discoverer.DiscoverAsync(request.PlatformRoot, cancellationToken);
        if (projects.Count == 0)
        {
            blocking.Add("No deployable projects were discovered.");
        }

        var publishingPathsCheck = CheckPublishingPaths(publishingSettings, outputDirectory);
        checks.Add(publishingPathsCheck);
        if (!publishingPathsCheck.Passed)
        {
            blocking.Add($"{publishingPathsCheck.Category}: {publishingPathsCheck.Message}");
            blocking.AddRange(publishingPathsCheck.Details);
        }

        var profileCheck = CheckPublishProfiles(publishingSettings);
        checks.Add(profileCheck);
        if (!profileCheck.Passed)
        {
            blocking.Add($"{profileCheck.Category}: {profileCheck.Message}");
            blocking.AddRange(profileCheck.Details);
        }
        else
        {
            warnings.AddRange(profileCheck.Details.Where(detail => detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
        }

        var releaseLibraryCheck = CheckReleaseLibrary(outputDirectory, projects);
        checks.Add(releaseLibraryCheck);
        if (!releaseLibraryCheck.Passed)
        {
            blocking.Add($"{releaseLibraryCheck.Category}: {releaseLibraryCheck.Message}");
            blocking.AddRange(releaseLibraryCheck.Details);
        }

        var drvCheck = CheckDeploymentValidation(publishingSettings);
        checks.Add(drvCheck);
        if (!drvCheck.Passed)
        {
            blocking.Add($"{drvCheck.Category}: {drvCheck.Message}");
            blocking.AddRange(drvCheck.Details.Where(detail => !detail.StartsWith("DRV enabled", StringComparison.OrdinalIgnoreCase)));
        }

        var dockerfileCheck = CheckDockerfiles(projects, publishingSettings);
        checks.Add(dockerfileCheck);
        if (!dockerfileCheck.Passed)
        {
            blocking.Add($"{dockerfileCheck.Category}: {dockerfileCheck.Message}");
            blocking.AddRange(dockerfileCheck.Details.Where(detail => !detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            warnings.AddRange(dockerfileCheck.Details.Where(detail => detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
        }

        var dotnetCheck = await CheckDotNetAsync(publishingSettings, cancellationToken);
        checks.Add(dotnetCheck);
        if (!dotnetCheck.Passed)
        {
            blocking.Add($"{dotnetCheck.Category}: {dotnetCheck.Message}");
            blocking.AddRange(dotnetCheck.Details);
        }

        var dockerCheck = await CheckDockerAsync(publishingSettings, request.SkipDockerBuildTest, cancellationToken);
        checks.Add(dockerCheck);
        if (!dockerCheck.Passed)
        {
            blocking.Add($"{dockerCheck.Category}: {dockerCheck.Message}");
            blocking.AddRange(dockerCheck.Details.Where(detail =>
                detail.Contains("fail", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("not appear", StringComparison.OrdinalIgnoreCase)));
        }

        var angularCheck = await CheckAngularAsync(projects, publishingSettings, request.SkipNpmRestore, cancellationToken);
        checks.Add(angularCheck);
        if (!angularCheck.Passed)
        {
            blocking.Add($"{angularCheck.Category}: {angularCheck.Message}");
            blocking.AddRange(angularCheck.Details.Where(detail =>
                detail.Contains("fail", StringComparison.OrdinalIgnoreCase)
                && !detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            warnings.AddRange(angularCheck.Details.Where(detail => detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
        }

        if (!request.SkipSolutionBuild && projects.Count > 0)
        {
            var solutionCheck = await CheckSolutionBuildAsync(projects, publishingSettings, projectWarnings, cancellationToken);
            checks.Add(solutionCheck);
            if (!solutionCheck.Passed)
            {
                blocking.Add($"{solutionCheck.Category}: {solutionCheck.Message}");
                blocking.AddRange(solutionCheck.Details.Where(detail => !detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                warnings.AddRange(solutionCheck.Details.Where(detail => detail.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)));
            }
        }

        var validationMode = ResolveValidationMode(request.DeploymentValidationMode, publishingSettings);
        var diskBudget = EstimateDiskBudget(projects.Count, publishingSettings, validationMode);
        var diskCheck = CheckDiskSpace(outputDirectory, publishingSettings, diskBudget);
        checks.Add(diskCheck);
        if (!diskCheck.Passed)
        {
            blocking.Add($"{diskCheck.Category}: {diskCheck.Message}");
            blocking.AddRange(diskCheck.Details.Where(detail => detail.Contains("Insufficient", StringComparison.OrdinalIgnoreCase)));
        }

        stopwatch.Stop();
        var isReady = blocking.Count == 0;
        var verdict = isReady ? "READY TO PUBLISH" : "BLOCKED";
        var summary = isReady
            ? $"All {checks.Count} preflight categories passed. Projected peak disk: {FormatBytes(diskBudget.MaximumPeakBytes)}."
            : $"Preflight blocked by {blocking.Count} issue(s). Fix blocking reasons before publishing.";

        logger.LogInformation("Preflight {Verdict} in {Seconds:F1}s", verdict, stopwatch.Elapsed.TotalSeconds);

        return new SolutionPublishingPreflightResult
        {
            IsReady = isReady,
            Verdict = verdict,
            Summary = summary,
            Checks = checks,
            BlockingReasons = blocking,
            Warnings = warnings,
            ProjectWarningCounts = projectWarnings,
            EstimatedRequiredBytes = diskBudget.MaximumPeakBytes,
            AvailableBytes = diskCheck.Passed ? ParseAvailableBytes(diskCheck.Details) : 0,
            Duration = stopwatch.Elapsed
        };
    }

    private SolutionPreflightCheckResult CheckPublishingPaths(PublishingSettings publishingSettings, string outputDirectory)
    {
        var details = new List<string>();
        var paths = new (string Name, string Path)[]
        {
            ("OutputFolder", outputDirectory),
            ("PublishRoot", publishingSettings.PublishRoot),
            ("ArtifactRoot", publishingSettings.ArtifactRoot),
            ("TemporaryFolder", publishingSettings.TemporaryFolder),
            ("DeploymentValidationFolder", publishingSettings.DeploymentValidationFolder)
        };

        var passed = true;
        foreach (var (name, path) in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                passed = false;
                details.Add($"{name} is not configured.");
                continue;
            }

            if (OperatingSystem.IsWindows() && path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal))
            {
                passed = false;
                details.Add(
                    $"{name} uses Linux path '{path}' on Windows. Use ActiveProfile=local for local publishing "
                    + "(deploymentActiveProfile=demo still produces demo-ready artifacts).");
                continue;
            }

            if (!TryEnsureWritable(path, out var error))
            {
                passed = false;
                details.Add($"{name} is not writable: {error}");
            }
        }

        return new SolutionPreflightCheckResult
        {
            Category = "Publish Profiles",
            Passed = passed,
            Message = passed ? "Publishing paths are writable." : "Publishing paths failed validation.",
            Details = details
        };
    }

    private static SolutionPreflightCheckResult CheckPublishProfiles(PublishingSettings publishingSettings)
    {
        var details = new List<string>();
        var passed = true;

        try
        {
            var deploymentProfile = DeploymentPathResolver.Resolve(publishingSettings);
            details.Add($"DeploymentActiveProfile={publishingSettings.DeploymentActiveProfile}");
            details.Add($"Environment={deploymentProfile.DotNetEnvironment}");
            details.Add($"DockerNetwork={deploymentProfile.DockerNetwork}");
            details.Add($"ConfigRoot={deploymentProfile.ConfigRoot}");
            details.Add($"PackagesRoot={deploymentProfile.PackagesRoot}");

            foreach (var (name, value) in new[]
                     {
                         ("ConfigRoot", deploymentProfile.ConfigRoot),
                         ("PackagesRoot", deploymentProfile.PackagesRoot),
                         ("LogsRoot", deploymentProfile.LogsRoot)
                     })
            {
                if (!value.StartsWith('/'))
                {
                    passed = false;
                    details.Add($"Deployment profile container path {name} must be Linux absolute but was '{value}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(deploymentProfile.DockerNetwork))
            {
                passed = false;
                details.Add("Deployment profile DockerNetwork is required.");
            }

            if (string.IsNullOrWhiteSpace(publishingSettings.DefaultNetworkName))
            {
                passed = false;
                details.Add("Publishing DefaultNetworkName is required.");
            }
            else if (!string.Equals(publishingSettings.DefaultNetworkName, deploymentProfile.DockerNetwork, StringComparison.OrdinalIgnoreCase))
            {
                details.Add(
                    $"WARN: Publishing network '{publishingSettings.DefaultNetworkName}' differs from deployment profile network '{deploymentProfile.DockerNetwork}'.");
            }
        }
        catch (Exception ex)
        {
            passed = false;
            details.Add($"Unable to resolve deployment profile '{publishingSettings.DeploymentActiveProfile}': {ex.Message}");
        }

        details.Add($"ValidationProfile={publishingSettings.ValidationProfile}");

        return new SolutionPreflightCheckResult
        {
            Category = "Demo Profile",
            Passed = passed,
            Message = passed ? "Deployment profile resolved." : "Deployment profile validation failed.",
            Details = details
        };
    }

    private static SolutionPreflightCheckResult CheckReleaseLibrary(string outputDirectory, IReadOnlyList<DeployableProject> projects)
    {
        var details = new List<string>();
        var passed = true;

        try
        {
            Directory.CreateDirectory(outputDirectory);
            details.Add($"Release library root: {outputDirectory}");
        }
        catch (Exception ex)
        {
            passed = false;
            details.Add($"Unable to create release library root: {ex.Message}");
            return BuildCheck("Release Library", passed, details);
        }

        var categories = projects
            .Select(project => project.ReleaseLibraryCategory)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            var categoryPath = Path.Combine(outputDirectory, category);
            try
            {
                Directory.CreateDirectory(categoryPath);
                details.Add($"Category ready: {category}");
            }
            catch (Exception ex)
            {
                passed = false;
                details.Add($"Unable to create category directory '{category}': {ex.Message}");
            }
        }

        return BuildCheck("Release Library", passed, details);
    }

    private static SolutionPreflightCheckResult CheckDeploymentValidation(PublishingSettings publishingSettings)
    {
        var details = new List<string>();
        var passed = true;

        if (!publishingSettings.EnableDeploymentReadinessValidation)
        {
            passed = false;
            details.Add("Deployment Readiness Validation (DRV) is disabled. Enable EnableDeploymentReadinessValidation.");
            return BuildCheck("Deployment Validation", passed, details);
        }

        details.Add("DRV enabled.");

        if (!TryEnsureWritable(publishingSettings.DeploymentValidationFolder, out var validationError))
        {
            passed = false;
            details.Add($"Validation workspace not writable: {validationError}");
        }
        else
        {
            details.Add($"Validation workspace writable: {publishingSettings.DeploymentValidationFolder}");
        }

        var probeRoot = Path.Combine(publishingSettings.TemporaryFolder, "preflight-probe");
        try
        {
            Directory.CreateDirectory(probeRoot);
            var probeFile = Path.Combine(probeRoot, Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(probeFile, "preflight");
            File.Delete(probeFile);
            Directory.Delete(probeRoot, recursive: true);
            details.Add($"Temporary deployment workspace creatable under {publishingSettings.TemporaryFolder}");
        }
        catch (Exception ex)
        {
            passed = false;
            details.Add($"Temporary deployment workspace not creatable: {ex.Message}");
        }

        return BuildCheck("Deployment Validation", passed, details);
    }

    private static SolutionPreflightCheckResult CheckDockerfiles(
        IReadOnlyList<DeployableProject> projects,
        PublishingSettings publishingSettings)
    {
        var details = new List<string>();
        var passed = true;

        foreach (var project in projects)
        {
            var projectRoot = Directory.Exists(project.DiscoveryPath)
                ? project.DiscoveryPath
                : Path.GetDirectoryName(project.ProjectPath) ?? project.DiscoveryPath;

            var dockerfile = FindDockerfile(projectRoot, project);
            var dockerignore = Path.Combine(Path.GetDirectoryName(dockerfile) ?? projectRoot, ".dockerignore");
            var hasDockerignore = File.Exists(dockerignore);

            if (dockerfile is not null)
            {
                details.Add($"{project.ServiceId}: project Dockerfile present ({dockerfile})");
            }
            else
            {
                var template = ResolveDockerfileTemplate(project.Type, publishingSettings);
                if (File.Exists(template))
                {
                    details.Add($"{project.ServiceId}: using APE template {Path.GetFileName(template)}");
                }
                else
                {
                    passed = false;
                    details.Add($"{project.ServiceId}: missing Dockerfile and template {template}");
                }
            }

            if (!hasDockerignore)
            {
                details.Add($"WARN: {project.ServiceId}: .dockerignore not found near {projectRoot}");
            }
        }

        return BuildCheck("Dockerfiles", passed, details);
    }

    private async Task<SolutionPreflightCheckResult> CheckDotNetAsync(
        PublishingSettings publishingSettings,
        CancellationToken cancellationToken)
    {
        var details = new List<string>();
        var passed = true;
        var timeout = publishingSettings.StepTimeoutSeconds;

        var sdk = await processRunner.RunAsync(
            publishingSettings.DotNetExecutable,
            "--version",
            Environment.CurrentDirectory,
            timeout,
            cancellationToken);
        if (sdk.IsSuccess)
        {
            details.Add($"SDK: {sdk.StandardOutput.Trim()}");
        }
        else
        {
            passed = false;
            details.Add($"dotnet --version failed: {sdk.StandardError.Trim()}");
        }

        var workloads = await processRunner.RunAsync(
            publishingSettings.DotNetExecutable,
            "workload list",
            Environment.CurrentDirectory,
            timeout,
            cancellationToken);
        if (workloads.IsSuccess)
        {
            details.Add("Workloads listed successfully.");
            var firstLine = workloads.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (firstLine is not null)
            {
                details.Add(firstLine.Trim());
            }
        }
        else
        {
            details.Add($"WARN: dotnet workload list failed: {workloads.StandardError.Trim()}");
        }

        var runtimes = await processRunner.RunAsync(
            publishingSettings.DotNetExecutable,
            "--list-runtimes",
            Environment.CurrentDirectory,
            timeout,
            cancellationToken);
        if (runtimes.IsSuccess)
        {
            var runtimeLines = runtimes.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Contains("Microsoft.", StringComparison.OrdinalIgnoreCase))
                .Take(3);
            details.Add("Runtimes available.");
            details.AddRange(runtimeLines.Select(line => line.Trim()));
        }
        else
        {
            passed = false;
            details.Add($"dotnet --list-runtimes failed: {runtimes.StandardError.Trim()}");
        }

        return BuildCheck(".NET", passed, details);
    }

    private async Task<SolutionPreflightCheckResult> CheckDockerAsync(
        PublishingSettings publishingSettings,
        bool skipBuildTest,
        CancellationToken cancellationToken)
    {
        var details = new List<string>();
        var passed = true;
        var timeout = publishingSettings.StepTimeoutSeconds;
        var docker = publishingSettings.DockerExecutable;

        var version = await processRunner.RunAsync(docker, "version", Environment.CurrentDirectory, timeout, cancellationToken);
        if (version.IsSuccess)
        {
            details.Add("docker version OK");
            details.Add(version.StandardOutput.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty);
        }
        else
        {
            passed = false;
            details.Add("Docker Desktop does not appear to be running (docker version failed).");
            details.Add(version.StandardError.Trim());
            return BuildCheck("Docker", passed, details);
        }

        var info = await processRunner.RunAsync(docker, "info", Environment.CurrentDirectory, timeout, cancellationToken);
        if (info.IsSuccess)
        {
            details.Add("docker info OK");
        }
        else
        {
            passed = false;
            details.Add($"docker info failed: {info.StandardError.Trim()}");
        }

        var compose = await processRunner.RunAsync(docker, "compose version", Environment.CurrentDirectory, timeout, cancellationToken);
        if (compose.IsSuccess)
        {
            details.Add($"docker compose: {compose.StandardOutput.Trim()}");
        }
        else
        {
            passed = false;
            details.Add($"docker compose version failed: {compose.StandardError.Trim()}");
        }

        if (!skipBuildTest && passed)
        {
            var buildCheck = await RunDockerBuildProbeAsync(docker, publishingSettings.TemporaryFolder, timeout, cancellationToken);
            details.AddRange(buildCheck.Details);
            if (!buildCheck.Passed)
            {
                passed = false;
            }
        }

        return BuildCheck("Docker", passed, details);
    }

    private async Task<SolutionPreflightCheckResult> CheckAngularAsync(
        IReadOnlyList<DeployableProject> projects,
        PublishingSettings publishingSettings,
        bool skipNpmRestore,
        CancellationToken cancellationToken)
    {
        var details = new List<string>();
        var passed = true;
        var timeout = publishingSettings.StepTimeoutSeconds;
        var portal = projects.FirstOrDefault(project => project.Type == PublishingProjectType.Angular);
        // DiscoveryPath/ProjectPath for an Angular project is angular.json (its manifest file, the
        // same discovery-path convention .csproj-based projects use) — never a directory itself.
        // Every process launched here needs a directory to run in, so resolve one the same
        // defensive way FindDockerfile's caller already does above (Directory.Exists guard,
        // falling back to the containing directory) rather than assuming DiscoveryPath is usable
        // as-is.
        var npmWorkingDirectory = portal is null
            ? Environment.CurrentDirectory
            : Directory.Exists(portal.DiscoveryPath)
                ? portal.DiscoveryPath
                : Path.GetDirectoryName(portal.ProjectPath) ?? Environment.CurrentDirectory;

        var node = await processRunner.RunAsync(
            publishingSettings.NodeExecutable,
            "--version",
            npmWorkingDirectory,
            timeout,
            cancellationToken);
        if (node.IsSuccess)
        {
            details.Add($"node {node.StandardOutput.Trim()}");
        }
        else
        {
            passed = false;
            details.Add($"node --version failed: {node.StandardError.Trim()}");
        }

        var npm = await RunToolAsync(
            publishingSettings.NpmExecutable,
            "--version",
            npmWorkingDirectory,
            timeout,
            cancellationToken);
        if (npm.IsSuccess)
        {
            details.Add($"npm {npm.StandardOutput.Trim()}");
        }
        else
        {
            passed = false;
            details.Add($"npm --version failed: {TrimOutput(npm)}");
        }

        var ng = await RunToolAsync(
            publishingSettings.NpmExecutable,
            "exec -- ng version",
            npmWorkingDirectory,
            timeout,
            cancellationToken);
        if (ng.IsSuccess)
        {
            details.Add("Angular CLI available.");
        }
        else
        {
            details.Add($"WARN: Angular CLI check failed: {ng.StandardError.Trim()}");
        }

        if (!skipNpmRestore && portal is not null && passed)
        {
            var restore = await RunToolAsync(
                publishingSettings.NpmExecutable,
                "ci",
                npmWorkingDirectory,
                timeout,
                cancellationToken);
            if (restore.IsSuccess)
            {
                details.Add($"npm ci succeeded for portal at {npmWorkingDirectory}");
            }
            else
            {
                passed = false;
                details.Add($"npm ci failed for portal: {restore.StandardError.Trim()}");
            }
        }

        return BuildCheck("Angular", passed, details);
    }

    private async Task<SolutionPreflightCheckResult> CheckSolutionBuildAsync(
        IReadOnlyList<DeployableProject> projects,
        PublishingSettings publishingSettings,
        Dictionary<string, int> projectWarnings,
        CancellationToken cancellationToken)
    {
        var details = new List<string>();
        var passed = true;
        var timeout = publishingSettings.StepTimeoutSeconds;

        foreach (var project in projects.Where(item => item.Type != PublishingProjectType.Angular))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projectPath = project.ProjectPath;
            if (!File.Exists(projectPath))
            {
                passed = false;
                details.Add($"{project.ServiceId}: project file not found: {projectPath}");
                continue;
            }

            var restore = await processRunner.RunAsync(
                publishingSettings.DotNetExecutable,
                $"restore \"{projectPath}\" --nologo",
                Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory,
                timeout,
                cancellationToken);
            if (!restore.IsSuccess)
            {
                passed = false;
                details.Add($"{project.ServiceId}: restore failed: {TrimOutput(restore)}");
                continue;
            }

            var build = await processRunner.RunAsync(
                publishingSettings.DotNetExecutable,
                $"build \"{projectPath}\" -c Release --no-restore --nologo",
                Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory,
                timeout,
                cancellationToken);
            if (!build.IsSuccess)
            {
                passed = false;
                details.Add($"{project.ServiceId}: build failed: {TrimOutput(build)}");
                continue;
            }

            var warningCount = CountWarnings(build.StandardOutput) + CountWarnings(build.StandardError);
            if (warningCount > 0)
            {
                projectWarnings[project.ServiceId] = warningCount;
                details.Add($"WARN: {project.ServiceId}: {warningCount} build warning(s)");
            }
            else
            {
                details.Add($"{project.ServiceId}: build succeeded with 0 warnings");
            }
        }

        return BuildCheck("Solution", passed, details);
    }

    private static SolutionPreflightCheckResult CheckDiskSpace(
        string outputDirectory,
        PublishingSettings publishingSettings,
        DrvDiskBudgetEstimate diskBudget)
    {
        var details = new List<string>();
        var passed = true;

        var probePath = outputDirectory;
        if (string.IsNullOrWhiteSpace(probePath))
        {
            probePath = publishingSettings.TemporaryFolder;
        }

        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(probePath)) ?? probePath;
            var drive = new DriveInfo(root);
            var available = drive.AvailableFreeSpace;
            details.Add($"Required workspace: {FormatBytes(diskBudget.RequiredBytes)}");
            details.Add($"Safe margin: {FormatBytes(diskBudget.SafeMarginBytes)}");
            details.Add($"Maximum projected peak: {FormatBytes(diskBudget.MaximumPeakBytes)}");
            details.Add($"Available on {drive.Name}: {FormatBytes(available)}");
            details.Add($"available-bytes={available}");

            if (available < diskBudget.MaximumPeakBytes)
            {
                passed = false;
                details.Add(
                    $"Insufficient disk space. Projected peak {FormatBytes(diskBudget.MaximumPeakBytes)} exceeds available {FormatBytes(available)}.");
            }
        }
        catch (Exception ex)
        {
            passed = false;
            details.Add($"Unable to estimate disk space: {ex.Message}");
        }

        return BuildCheck("Disk Space", passed, details);
    }

    private async Task<SolutionPreflightCheckResult> RunDockerBuildProbeAsync(
        string dockerExecutable,
        string temporaryFolder,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var details = new List<string>();
        var passed = true;
        var probeRoot = Path.GetFullPath(Path.Combine(temporaryFolder, "preflight-docker-probe", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(probeRoot);

        try
        {
            var dockerfile = Path.Combine(probeRoot, "Dockerfile");
            await File.WriteAllTextAsync(
                dockerfile,
                """
                FROM alpine:3.19
                RUN echo aovias-preflight-ok
                """,
                cancellationToken);

            var build = await processRunner.RunAsync(
                dockerExecutable,
                $"build -t aovias/preflight-probe:local \"{probeRoot.Replace('\\', '/').Replace("\"", "\\\"")}\"",
                probeRoot,
                timeoutSeconds,
                cancellationToken);

            if (build.IsSuccess)
            {
                details.Add("Docker image build capability verified.");
            }
            else
            {
                passed = false;
                details.Add($"Docker build probe failed: {TrimOutput(build)}");
            }
        }
        catch (Exception ex)
        {
            passed = false;
            details.Add($"Docker build probe failed: {ex.Message}");
        }
        finally
        {
            try
            {
                if (Directory.Exists(probeRoot))
                {
                    Directory.Delete(probeRoot, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        return new SolutionPreflightCheckResult
        {
            Category = "Docker Build",
            Passed = passed,
            Message = passed ? "Docker build probe succeeded." : "Docker build probe failed.",
            Details = details
        };
    }

    private static string? FindDockerfile(string projectRoot, DeployableProject project)
    {
        var candidates = new List<string>
        {
            Path.Combine(projectRoot, "Dockerfile"),
            Path.Combine(Path.GetDirectoryName(project.ProjectPath) ?? projectRoot, "Dockerfile")
        };

        if (project.ServiceId.Equals("runtime-agent", StringComparison.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(projectRoot);
            while (current is not null)
            {
                candidates.Add(Path.Combine(current.FullName, "Aovias.Runtime.Agent", "Dockerfile"));
                candidates.Add(Path.Combine(current.FullName, "Dockerfile"));
                if (RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot(current.FullName)
                    && Directory.Exists(Path.Combine(current.FullName, "ComplaincePlatform")))
                {
                    break;
                }

                current = current.Parent;
            }
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).FirstOrDefault(File.Exists);
    }

    private async Task<ProcessExecutionResult> RunToolAsync(
        string executable,
        string arguments,
        string workingDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        ProcessExecutionResult? last = null;
        foreach (var candidate in ResolveExecutableCandidates(executable))
        {
            last = await processRunner.RunAsync(candidate, arguments, workingDirectory, timeoutSeconds, cancellationToken);
            if (last.ExitCode != -1 || !last.StandardError.Contains("Unable to start", StringComparison.OrdinalIgnoreCase))
            {
                return last;
            }
        }

        return last ?? new ProcessExecutionResult
        {
            ExitCode = -1,
            StandardError = $"Unable to start '{executable}'."
        };
    }

    private static IEnumerable<string> ResolveExecutableCandidates(string executable)
    {
        if (OperatingSystem.IsWindows() && executable.Equals("npm", StringComparison.OrdinalIgnoreCase))
        {
            var programFilesNpm = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "nodejs",
                "npm.cmd");
            if (File.Exists(programFilesNpm))
            {
                yield return programFilesNpm;
            }

            yield return "npm.cmd";
            yield return executable;
            yield break;
        }

        yield return executable;

        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        if (executable.Equals("node", StringComparison.OrdinalIgnoreCase))
        {
            var programFilesNode = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "nodejs",
                "node.exe");
            if (File.Exists(programFilesNode))
            {
                yield return programFilesNode;
            }

            yield return "node.exe";
        }
    }

    private static DeploymentValidationMode ResolveValidationMode(string? requestedMode, PublishingSettings settings)
        => Enum.TryParse<DeploymentValidationMode>(requestedMode ?? settings.DeploymentValidationMode, ignoreCase: true, out var parsed)
            ? parsed
            : DeploymentValidationMode.Balanced;

    private static DrvDiskBudgetEstimate EstimateDiskBudget(
        int projectCount,
        PublishingSettings settings,
        DeploymentValidationMode mode)
    {
        var budget = DrvResourceMonitor.EstimateDiskBudget(projectCount, mode);
        if (settings.SequentialDeploymentValidation)
        {
            return budget;
        }

        return new DrvDiskBudgetEstimate
        {
            ServiceCount = budget.ServiceCount,
            SharedWorkspaceBytes = budget.SharedWorkspaceBytes,
            PerServicePeakBytes = budget.PerServicePeakBytes + (projectCount * 500L * 1024 * 1024),
            PublishWorkingSetBytes = budget.PublishWorkingSetBytes,
            SafeMarginBytes = budget.SafeMarginBytes
        };
    }

    private static string ResolveDockerfileTemplate(PublishingProjectType type, PublishingSettings settings)
        => type switch
        {
            PublishingProjectType.WorkerService => settings.DockerfileWorkerTemplatePath,
            PublishingProjectType.Angular => settings.DockerfileAngularTemplatePath,
            PublishingProjectType.StaticWeb => settings.DockerfileStaticTemplatePath,
            _ => settings.DockerfileAspNetTemplatePath
        };

    private static bool TryEnsureWritable(string path, out string error)
    {
        error = string.Empty;
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".preflight-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static int CountWarnings(string output)
        => output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(line => WarningRegex.IsMatch(line));

    private static string TrimOutput(ProcessExecutionResult result)
    {
        var text = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        return text.Length <= 500 ? text.Trim() : text[..500].Trim() + "...";
    }

    private static long ParseAvailableBytes(IReadOnlyList<string> details)
    {
        var line = details.FirstOrDefault(item => item.StartsWith("available-bytes=", StringComparison.OrdinalIgnoreCase));
        return line is not null && long.TryParse(line.Split('=')[1], out var value) ? value : 0;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }

    private static SolutionPreflightCheckResult BuildCheck(string category, bool passed, List<string> details)
        => new()
        {
            Category = category,
            Passed = passed,
            Message = passed ? $"{category} checks passed." : $"{category} checks failed.",
            Details = details
        };

    private static void Report(IProgress<SolutionPublishingPreflightProgress>? progress, string category, string message)
        => progress?.Report(new SolutionPublishingPreflightProgress { Category = category, Message = message });
}
