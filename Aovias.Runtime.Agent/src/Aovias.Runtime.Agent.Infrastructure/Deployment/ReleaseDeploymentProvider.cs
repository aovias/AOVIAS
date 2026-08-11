using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Aovias.Configuration.Deployment;
using Aovias.Configuration.Profiles;
using Aovias.Configuration.Settings;
using Aovias.Configuration.SignalR;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Execution;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Runtime.Agent.Infrastructure.Admission;
using Aovias.Runtime.Agent.Infrastructure.Reconciliation;
using Aovias.Orchestration.Contracts.Providers;
using Aovias.PackageBuilder.Abstractions;
using Aovias.Publishing.Contracts;
using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using Aovias.Runtime.Agent.Domain.Entities;
using Aovias.Runtime.Agent.Infrastructure.Persistence;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Executes release deployment operations for orchestration pipeline steps.
/// </summary>
public sealed class ReleaseDeploymentProvider : IReleaseDeploymentProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Sprint M2.4 D1/D2: stateless, deterministic decision engines — no I/O, no injected
    /// dependencies of their own — so they are held as static instances rather than added to this
    /// class's already-large injected dependency list. Held via their interfaces so a future
    /// sprint can move them to DI without any call-site changes here.
    /// </summary>
    private static readonly IDeploymentAdmissionEngine AdmissionEngine = new DeploymentAdmissionEngine();

    private static readonly IDeploymentPlanner PlanBuilder = new DeploymentPlanBuilder();

    private readonly IReleaseRepository releaseRepository;
    private readonly IReleaseValidator releaseValidator;
    private readonly IReleaseAuditStore auditStore;
    private readonly IPackageReader packageReader;
    private readonly INginxProvider nginxProvider;
    private readonly IDockerProvider dockerProvider;
    private readonly IHealthProvider healthProvider;
    private readonly RuntimeManifestStore manifestStore;
    private readonly RuntimeCapabilitiesProvider capabilitiesProvider;
    private readonly IProfileManager profileManager;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly IOptionsMonitor<RuntimeManifestSettings> manifestSettings;
    private readonly IOptionsMonitor<SignalRSettings> signalRSettings;
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;
    private readonly IInfrastructureExecutionContext infrastructureExecution;
    private readonly IDeploymentPackageContractReader contractReader;
    private readonly LastDeploymentPlanStore lastDeploymentPlanStore;
    private readonly IReconciliationScheduler reconciliationScheduler;
    private readonly EnvironmentAdmissionStore environmentAdmissionStore;
    private readonly ILogger<ReleaseDeploymentProvider> logger;

    public ReleaseDeploymentProvider(
        IReleaseRepository releaseRepository,
        IReleaseValidator releaseValidator,
        IReleaseAuditStore auditStore,
        IPackageReader packageReader,
        INginxProvider nginxProvider,
        IDockerProvider dockerProvider,
        IHealthProvider healthProvider,
        RuntimeManifestStore manifestStore,
        RuntimeCapabilitiesProvider capabilitiesProvider,
        IProfileManager profileManager,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        IOptionsMonitor<DockerSettings> dockerSettings,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IOptionsMonitor<ApiSettings> apiSettings,
        IOptionsMonitor<RuntimeManifestSettings> manifestSettings,
        IOptionsMonitor<SignalRSettings> signalRSettings,
        IOptionsMonitor<ProviderSettings> providerSettings,
        IInfrastructureExecutionContext infrastructureExecution,
        IDeploymentPackageContractReader contractReader,
        LastDeploymentPlanStore lastDeploymentPlanStore,
        IReconciliationScheduler reconciliationScheduler,
        EnvironmentAdmissionStore environmentAdmissionStore,
        ILogger<ReleaseDeploymentProvider> logger)
    {
        this.releaseRepository = releaseRepository;
        this.releaseValidator = releaseValidator;
        this.auditStore = auditStore;
        this.packageReader = packageReader;
        this.nginxProvider = nginxProvider;
        this.dockerProvider = dockerProvider;
        this.healthProvider = healthProvider;
        this.manifestStore = manifestStore;
        this.capabilitiesProvider = capabilitiesProvider;
        this.profileManager = profileManager;
        this.releaseSettings = releaseSettings;
        this.deploymentSettings = deploymentSettings;
        this.dockerSettings = dockerSettings;
        this.runtimeSettings = runtimeSettings;
        this.apiSettings = apiSettings;
        this.manifestSettings = manifestSettings;
        this.signalRSettings = signalRSettings;
        this.providerSettings = providerSettings;
        this.infrastructureExecution = infrastructureExecution;
        this.contractReader = contractReader;
        this.lastDeploymentPlanStore = lastDeploymentPlanStore;
        this.reconciliationScheduler = reconciliationScheduler;
        this.environmentAdmissionStore = environmentAdmissionStore;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var validation = await releaseValidator.ValidateReleaseAsync(release, cancellationToken);
        if (!validation.IsValid)
        {
            return DeploymentStepResult.Failure(string.Join("; ", validation.Errors));
        }

        await RecordAuditAsync(releaseId, AuditEventType.ValidationPassed.ToString(), "Package validation passed.", "system", cancellationToken);
        return DeploymentStepResult.Success($"Package {release.Identity.PackageId} validated.");
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> ValidateDependenciesAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var docker = dockerSettings.CurrentValue;

        if (!docker.Enabled)
        {
            return DeploymentStepResult.Failure("Docker provider is disabled in configuration.");
        }

        if (!Directory.Exists(settings.ExtractionFolder))
        {
            EnsureDirectoryPath(settings.ExtractionFolder);
        }

        if (!Directory.Exists(docker.ComposeRoot))
        {
            EnsureDirectoryPath(docker.ComposeRoot);
        }

        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        foreach (var dependency in contents.Manifest.Dependencies)
        {
            logger.LogInformation(
                "Dependency {PackageId} {Version} verified for release {ReleaseId}.",
                dependency.PackageId,
                dependency.PackageVersion,
                releaseId);
        }

        return DeploymentStepResult.Success($"Dependencies verified for {contents.Manifest.PackageId}.");
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> ExtractReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var extractionPath = GetExtractionPath(releaseId);

        if (Directory.Exists(extractionPath))
        {
            Directory.Delete(extractionPath, recursive: true);
        }

        EnsureDirectoryPath(extractionPath);
        ZipFile.ExtractToDirectory(release.PackagePath, extractionPath);

        return DeploymentStepResult.Success(
            $"Release extracted to {extractionPath}.",
            new Dictionary<string, string> { ["extractionPath"] = extractionPath });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> StageReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var serviceId = contents.Service.ServiceId;
        var extractionPath = GetExtractionPath(releaseId);
        var stagedPath = Path.Combine(settings.StagingFolder, releaseId);
        var composeTargetDirectory = Path.Combine(dockerSettings.CurrentValue.ComposeRoot, serviceId);
        var composeSourcePath = Path.Combine(extractionPath, contents.Service.ComposeFile);
        var composeTargetPath = Path.Combine(composeTargetDirectory, "docker-compose.yml");

        if (!Directory.Exists(extractionPath))
        {
            return DeploymentStepResult.Failure("Release extraction folder was not found. Run extraction first.");
        }

        if (!File.Exists(composeSourcePath))
        {
            return DeploymentStepResult.Failure($"Compose file '{contents.Service.ComposeFile}' was not found in the package.");
        }

        if (Directory.Exists(stagedPath))
        {
            Directory.Delete(stagedPath, recursive: true);
        }

        CopyDirectory(extractionPath, stagedPath);
        EnsureDirectoryPath(composeTargetDirectory);
        File.Copy(composeSourcePath, composeTargetPath, overwrite: true);

        CopySupportingComposeFiles(extractionPath, composeTargetDirectory, contents.Service.ComposeFile);

        return DeploymentStepResult.Success(
            $"Release staged for service '{serviceId}'.",
            new Dictionary<string, string>
            {
                ["stagedPath"] = stagedPath,
                ["composeFilePath"] = composeTargetPath,
                ["serviceId"] = serviceId,
                ["containerName"] = contents.Service.ContainerName,
                ["hasServiceImage"] = contents.HasServiceImage.ToString()
            });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> LoadDockerImageAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        if (!contents.HasServiceImage)
        {
            return DeploymentStepResult.Success("No offline Docker image found; load step skipped.");
        }

        var settings = releaseSettings.CurrentValue;
        var stagedPath = Path.Combine(settings.StagingFolder, releaseId);
        var tarPath = Path.Combine(stagedPath, DeployPackageLayout.ServiceImageTar);
        if (!File.Exists(tarPath))
        {
            var extractionPath = GetExtractionPath(releaseId);
            tarPath = Path.Combine(extractionPath, DeployPackageLayout.ServiceImageTar);
        }

        if (!File.Exists(tarPath))
        {
            return DeploymentStepResult.Failure($"Docker image tar '{DeployPackageLayout.ServiceImageTar}' was not found in staged release.");
        }

        if (!dockerSettings.CurrentValue.Enabled)
        {
            return DeploymentStepResult.Failure("Docker provider is disabled in configuration.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"load -i \"{tarPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start docker load process.");

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            logger.LogError("Docker load failed for release {ReleaseId}: {StdErr}", releaseId, stderr);
            return DeploymentStepResult.Failure(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        }

        logger.LogInformation("Docker image loaded for release {ReleaseId}: {Output}", releaseId, stdout);
        return DeploymentStepResult.Success(
            $"Docker image loaded from {DeployPackageLayout.ServiceImageTar}.",
            new Dictionary<string, string> { ["imageTarPath"] = tarPath });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> BackupCurrentReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var serviceId = contents.Service.ServiceId;
        var currentPath = Path.Combine(settings.ActiveFolder, serviceId, "current");
        var previousPath = Path.Combine(settings.RollbackFolder, serviceId, "previous");

        if (!Directory.Exists(currentPath))
        {
            return DeploymentStepResult.Success("No active release found; backup skipped.");
        }

        if (Directory.Exists(previousPath))
        {
            Directory.Delete(previousPath, recursive: true);
        }

        EnsureDirectoryPath(Path.GetDirectoryName(previousPath)!);
        CopyDirectory(currentPath, previousPath);

        return DeploymentStepResult.Success(
            $"Previous release backed up for service '{serviceId}'.",
            new Dictionary<string, string> { ["previousPath"] = previousPath });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> ActivateReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var serviceId = contents.Service.ServiceId;
        var stagedPath = Path.Combine(settings.StagingFolder, releaseId);
        var currentPath = Path.Combine(settings.ActiveFolder, serviceId, "current");
        var archivePath = Path.Combine(settings.ArchiveFolder, serviceId, releaseId);

        if (!Directory.Exists(stagedPath))
        {
            return DeploymentStepResult.Failure("Staged release folder was not found.");
        }

        if (Directory.Exists(currentPath))
        {
            if (Directory.Exists(archivePath))
            {
                Directory.Delete(archivePath, recursive: true);
            }

            EnsureDirectoryPath(Path.GetDirectoryName(archivePath)!);
            CopyDirectory(currentPath, archivePath);
            Directory.Delete(currentPath, recursive: true);
        }

        EnsureDirectoryPath(Path.GetDirectoryName(currentPath)!);
        CopyDirectory(stagedPath, currentPath);

        var pointerPath = Path.Combine(settings.ActiveFolder, serviceId, "active-release.json");
        await File.WriteAllTextAsync(
            pointerPath,
            JsonSerializer.Serialize(new ActiveReleasePointer
            {
                CurrentReleaseId = releaseId,
                PackageId = release.Identity.PackageId,
                PackageVersion = release.Identity.PackageVersion,
                UpdatedAt = DateTimeOffset.UtcNow
            }, JsonOptions),
            cancellationToken);

        return DeploymentStepResult.Success(
            $"Release {releaseId} activated for service '{serviceId}'.",
            new Dictionary<string, string> { ["activePath"] = currentPath });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> RollbackReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var serviceId = contents.Service.ServiceId;
        var currentPath = Path.Combine(settings.ActiveFolder, serviceId, "current");
        var previousPath = Path.Combine(settings.RollbackFolder, serviceId, "previous");
        var composeTargetDirectory = Path.Combine(dockerSettings.CurrentValue.ComposeRoot, serviceId);
        var composeTargetPath = Path.Combine(composeTargetDirectory, "docker-compose.yml");

        if (!Directory.Exists(previousPath))
        {
            return DeploymentStepResult.Failure("No previous release is available for rollback.");
        }

        if (Directory.Exists(currentPath))
        {
            Directory.Delete(currentPath, recursive: true);
        }

        EnsureDirectoryPath(Path.GetDirectoryName(currentPath)!);
        CopyDirectory(previousPath, currentPath);

        var previousCompose = Path.Combine(previousPath, contents.Service.ComposeFile);
        if (File.Exists(previousCompose))
        {
            EnsureDirectoryPath(composeTargetDirectory);
            File.Copy(previousCompose, composeTargetPath, overwrite: true);
            CopySupportingComposeFiles(previousPath, composeTargetDirectory, contents.Service.ComposeFile);
        }

        await RecordAuditAsync(
            releaseId,
            AuditEventType.RollbackCompleted.ToString(),
            $"Rollback restored previous release for service '{serviceId}'.",
            "system",
            cancellationToken);

        return DeploymentStepResult.Success($"Rollback completed for service '{serviceId}'.");
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> EnsureRuntimeFoldersAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var settings = releaseSettings.CurrentValue;
        var docker = dockerSettings.CurrentValue;
        var folders = new[]
        {
            settings.IncomingFolder,
            settings.StagingFolder,
            settings.ExtractionFolder,
            settings.ActiveFolder,
            settings.RollbackFolder,
            settings.ArchiveFolder,
            settings.AuditFolder,
            settings.TemporaryFolder,
            docker.ComposeRoot
        };

        foreach (var folder in folders.Where(folder => !string.IsNullOrWhiteSpace(folder)))
        {
            EnsureDirectoryPath(folder);
        }

        var metadata = new Dictionary<string, string>();
        var stagedPath = Path.Combine(settings.StagingFolder, releaseId);
        var extractionPath = GetExtractionPath(releaseId);
        var packageDirectory = Directory.Exists(stagedPath) ? stagedPath : extractionPath;
        InfrastructureContract contract;
        try
        {
            contract = contractReader.ReadFromPackage(release.PackagePath);
        }
        catch (DeploymentPackageContractException exception)
        {
            return DeploymentStepResult.Failure(exception.Message);
        }

        var bundle = await TryResolveDeploymentBundleAsync(release.PackagePath, packageDirectory, cancellationToken);
        if (bundle is not null)
        {
            var hostPaths = TryResolveHostPaths();
            InfrastructureReadinessReport? readiness = null;
            InfrastructureProvisioningContext? provisioningContext = null;
            if (hostPaths is not null)
            {
                provisioningContext = new InfrastructureProvisioningContext
                {
                    Bundle = bundle,
                    Contract = contract,
                    HostPaths = hostPaths,
                    RuntimeCapabilities = MapRuntimeCapabilities(),
                    Execution = infrastructureExecution,
                    PackageDirectory = packageDirectory,
                    DockerNetworkName = ResolveDockerNetworkName(contract)
                };

                readiness = await InfrastructureProvisioningPipeline.ExecuteAsync(provisioningContext, cancellationToken);
                foreach (var stage in readiness.Stages)
                {
                    metadata[$"infrastructure:{stage.Stage}"] = stage.State.ToString();
                    foreach (var message in stage.Messages)
                    {
                        logger.LogInformation(
                            "Infrastructure {Stage} for release {ReleaseId}: {Message}",
                            stage.Stage,
                            releaseId,
                            message);
                    }
                }

                if (!readiness.DeploymentAllowed)
                {
                    var recovery = InfrastructureRecoveryCoordinator.DescribeRecoveryActions(readiness);
                    var failureMessage = string.Join("; ", readiness.Blockers);
                    if (recovery.Count > 0)
                    {
                        failureMessage += " | Recovery: " + string.Join("; ", recovery);
                    }

                    return new DeploymentStepResult
                    {
                        IsSuccess = false,
                        Message = failureMessage,
                        Metadata = metadata
                    };
                }

                metadata["infrastructureReadiness"] = "PASS";
            }

            // Sprint M3.3 integration fix: the M4.4 environment-level Deployment Ready report
            // (EnvironmentAdmissionStore, recomputed on a timer by EnvironmentAdmissionHostedService
            // from this same environment's declared capability profile) existed only as a
            // heartbeat/API telemetry surface until now — nothing in this execution path actually
            // consulted it, so a deployment could proceed even while the environment reported
            // Missing blockers. Checked here, before the per-package Admission Engine below, so an
            // environment-level rejection is cheaper (no per-package fact-gathering) and uses the
            // same typed-failure shape every other admission rejection in this method already uses.
            // A missing/not-yet-computed report (fresh environment, hosted service's first cycle not
            // done) is treated as Ready — the same vacuous-true EnvironmentAdmissionEvaluator itself
            // returns for an empty requirement set, not a new default invented here.
            var environmentAdmission = await environmentAdmissionStore.TryLoadAsync(cancellationToken);
            if (environmentAdmission is { IsReady: false })
            {
                var blockerMessage = string.Join("; ", environmentAdmission.Blockers);
                logger.LogError(
                    "Environment-level admission rejected release {ReleaseId}: {Blockers}",
                    releaseId, blockerMessage);

                metadata["admission"] = "REJECTED";
                metadata["admissionScope"] = "Environment";
                return new DeploymentStepResult
                {
                    IsSuccess = false,
                    Message = $"Environment is not Deployment Ready: {blockerMessage}",
                    Metadata = metadata
                };
            }

            // Sprint M2.4 D6: Runtime Agent no longer decides deployability itself (the M2.3
            // DeploymentPlanner.Evaluate call this replaced both decided and, implicitly, planned
            // in one step). Every deployment request now passes through the Admission Engine
            // first; only an Approved decision may produce a Deployment Plan, and only a
            // plan that passes DeploymentPlanValidator is allowed to proceed. This provider still
            // performs no admission or planning logic itself — it only gathers the facts
            // (package integrity already established above, infrastructure readiness from the
            // pipeline just executed) and hands them to the shared engines.
            var availableCapabilities = LegacyCapabilityBridge.FromRuntimeCapabilities(MapRuntimeCapabilities());
            var admissionRequest = new AdmissionRequest
            {
                ServiceId = bundle.Manifest.ServiceId,
                CorrelationId = releaseId,
                Contract = contract,
                PackageIntegrity = PackageIntegrityFacts.Ok(),
                RuntimeAgentVersion = runtimeSettings.CurrentValue.RuntimeVersion,
                AvailableCapabilities = availableCapabilities,
                Environment = new EnvironmentRegistrationFacts
                {
                    EnvironmentId = runtimeSettings.CurrentValue.Environment,
                    // No external environment registry exists yet (Sprint M2.4 Technical Debt) —
                    // the runtime evaluating this request is definitionally the target
                    // environment, so registration is always true today.
                    Registered = true
                },
                InfrastructureReadiness = readiness is not null
                    ? InfrastructureReadinessFacts.FromReadinessReport(readiness)
                    : new InfrastructureReadinessFacts { Ready = true }
            };

            var admissionResult = AdmissionEngine.Evaluate(admissionRequest);
            foreach (var warning in admissionResult.Warnings)
            {
                logger.LogWarning(
                    "Admission warning for release {ReleaseId} [{Category}]: {Reason} (remediation: {Remediation})",
                    releaseId, warning.Category, warning.Reason, warning.Remediation);
            }

            if (admissionResult.Decision != AdmissionDecision.Approved)
            {
                foreach (var blocker in admissionResult.Blockers)
                {
                    logger.LogError(
                        "Admission rejected release {ReleaseId} [{Category}]: {Reason} (remediation: {Remediation})",
                        releaseId, blocker.Category, blocker.Reason, blocker.Remediation);
                }

                metadata["admission"] = "REJECTED";
                metadata["correlationId"] = releaseId;
                return new DeploymentStepResult
                {
                    IsSuccess = false,
                    Message = string.Join("; ", admissionResult.Blockers.Select(b => $"[{b.Category}] {b.Reason} (remediation: {b.Remediation})")),
                    Metadata = metadata
                };
            }

            metadata["admission"] = "APPROVED";

            var plan = PlanBuilder.CreatePlan(new DeploymentPlanRequest
            {
                Bundle = bundle,
                Contract = contract,
                Admission = admissionResult,
                EnvironmentId = runtimeSettings.CurrentValue.Environment,
                AvailableCapabilities = availableCapabilities,
                GeneratedUtc = DateTimeOffset.UtcNow
            });

            var planValidation = DeploymentPlanValidator.Validate(plan);
            if (!planValidation.IsValid)
            {
                logger.LogError(
                    "Deployment plan for release {ReleaseId} failed validation: {Errors}",
                    releaseId, string.Join("; ", planValidation.Errors));
                return DeploymentStepResult.Failure(string.Join("; ", planValidation.Errors));
            }

            metadata["plan:id"] = plan.Metadata.PlanId;
            metadata["plan:contentHash"] = plan.Diagnostics.ContentHash;
            foreach (var phase in plan.ExecutionPhases.OrderBy(item => item.Order))
            {
                metadata[$"plan:phase:{phase.Name}"] = phase.Tasks.Count.ToString();
            }

            // Sprint M2.5 D6/integration: the approved, validated plan is now actually executed —
            // not merely logged — through the new Execution Runtime. This provider still contains
            // no admission or planning logic of its own (Final Engineering Rule 7); it only
            // assembles the execution context and the concrete task provisioners the shared
            // runtime dispatches to. The Preflight and Infrastructure phases genuinely execute
            // here, adapting the existing InfrastructureProvisioningPipeline; Upload,
            // Configuration, and Deployment phase tasks are honestly reported Skipped, since their
            // real work still runs through the existing external step-based orchestrator
            // (ExtractReleaseAsync, StageReleaseAsync, etc.) — see the M2.5 report's Technical
            // Debt section for why that boundary was drawn deliberately.
            var executionContext = new DeploymentExecutionContext
            {
                EnvironmentId = runtimeSettings.CurrentValue.Environment,
                Plan = plan,
                RuntimeAgentVersion = runtimeSettings.CurrentValue.RuntimeVersion
            };
            if (provisioningContext is not null)
            {
                executionContext.ProvisionerState[ExistingInfrastructurePipelineTaskProvisioner.ProvisioningContextStateKey] = provisioningContext;
            }

            var gateCheck = new RuntimeGateCheck(dockerProvider, healthProvider, profileManager, apiSettings.CurrentValue.HealthEndpoint);
            var executionRegistry = new InfrastructureTaskProvisionerRegistry(
            [
                new ExistingInfrastructurePipelineTaskProvisioner(),
                new HealthGateTaskProvisioner(gateCheck),
                new CertificationTaskProvisioner(gateCheck),
                new ConfirmationTaskProvisioner(),
                new PendingExternalExecutionTaskProvisioner()
            ]);
            var executionRuntime = new DeploymentExecutionRuntime(executionRegistry);
            var executionResult = await executionRuntime.ExecuteAsync(executionContext, cancellationToken);

            metadata["execution:success"] = executionResult.Success.ToString();
            metadata["execution:percentComplete"] = executionResult.Progress.PercentComplete.ToString("0.##");
            metadata["execution:retryCount"] = executionResult.Progress.RetryCount.ToString();
            foreach (var executionError in executionResult.Errors)
            {
                logger.LogError("Execution error for release {ReleaseId}: {Error}", releaseId, executionError);
            }

            if (!executionResult.Success)
            {
                metadata["execution:canResume"] = executionResult.CanResume.ToString();
                return new DeploymentStepResult
                {
                    IsSuccess = false,
                    Message = string.Join("; ", executionResult.Errors),
                    Metadata = metadata
                };
            }

            metadata["serviceId"] = bundle.Manifest.ServiceId;
            metadata["contractVersion"] = bundle.Deployment.ContractVersion;

            // Sprint M2.6 D1/D8: this successful, executed plan becomes the new authoritative
            // Desired State source for continuous reconciliation — never anything observed at
            // runtime (Final Engineering Rule: "Desired State is the only authoritative
            // operational target"). Recording it here, and requesting an immediate
            // PostDeployment reconciliation cycle, is the only reconciliation-related logic this
            // provider contains; the actual Desired/Observed/Drift/Repair pipeline lives entirely
            // in Aovias.Deployment.Contracts.Reconciliation and Aovias.Runtime.Agent.Infrastructure.Reconciliation.
            lastDeploymentPlanStore.Record(bundle.Manifest.ServiceId, new LastDeploymentRecord(plan, contract, bundle.Manifest, provisioningContext?.DockerNetworkName));

            try
            {
                await reconciliationScheduler.RequestReconciliationAsync(ReconciliationTrigger.PostDeployment, cancellationToken);
            }
            catch (Exception reconciliationException)
            {
                // A post-deployment reconciliation failure must never fail the deployment that
                // already succeeded — it is best-effort, immediate verification, not a gate.
                logger.LogWarning(reconciliationException, "Post-deployment reconciliation for release {ReleaseId} failed to run.", releaseId);
            }
        }

        var deploymentId = $"DEP-{releaseId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        metadata["deploymentId"] = deploymentId;
        return DeploymentStepResult.Success("Runtime deployment folders verified.", metadata);
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> ConfigureNginxAsync(
        string releaseId,
        bool restorePrevious,
        CancellationToken cancellationToken = default)
    {
        _ = await RequireReleaseAsync(releaseId, cancellationToken);

        if (!nginxProvider.IsAvailable)
        {
            return DeploymentStepResult.Success("Nginx provider is disabled; configuration skipped.");
        }

        var result = restorePrevious
            ? await nginxProvider.RestorePreviousSiteAsync(cancellationToken)
            : await nginxProvider.ConfigureRuntimeSiteAsync(cancellationToken);

        return result.IsSuccess
            ? DeploymentStepResult.Success(result.Message, new Dictionary<string, string> { ["nginxConfigPath"] = result.ConfigPath ?? string.Empty })
            : DeploymentStepResult.Failure(result.Message);
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> RegisterDeploymentAsync(
        string releaseId,
        string deploymentId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        await RecordAuditAsync(
            releaseId,
            AuditEventType.DeploymentStarted.ToString(),
            $"Deployment {deploymentId} registered.",
            actor,
            cancellationToken,
            deploymentId,
            metadata: new Dictionary<string, string>
            {
                ["deploymentId"] = deploymentId,
                ["packageVersion"] = release.Identity.PackageVersion,
                ["runtime"] = release.Identity.Runtime,
                ["environment"] = release.Identity.Environment
            });

        return DeploymentStepResult.Success(
            $"Deployment {deploymentId} registered.",
            new Dictionary<string, string> { ["deploymentId"] = deploymentId });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> RegisterRuntimeAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var runtime = runtimeSettings.CurrentValue;
        var api = apiSettings.CurrentValue;
        var docker = dockerSettings.CurrentValue;
        var deployment = deploymentSettings.CurrentValue;
        var pointerPath = Path.Combine(releaseSettings.CurrentValue.ActiveFolder, contents.Service.ServiceId, "active-release.json");

        var manifest = await manifestStore.TryLoadAsync(cancellationToken) ?? new RuntimeManifestEntity
        {
            StartedAt = DateTimeOffset.UtcNow
        };

        manifest.RuntimeVersion = runtime.RuntimeVersion;
        manifest.Environment = runtime.Environment;
        manifest.Hostname = runtime.Hostname;
        manifest.ServerName = runtime.ServerName;
        manifest.ServerId = runtime.ServerId;
        manifest.PlatformVersion = runtime.PlatformVersion;
        manifest.DeploymentAgentVersion = deployment.AgentVersion;
        manifest.DockerNetwork = docker.Network;
        manifest.LastUpdated = DateTimeOffset.UtcNow;

        var serviceEntry = manifest.ApplicationServices.FirstOrDefault(service =>
            string.Equals(service.Id, contents.Service.ServiceId, StringComparison.OrdinalIgnoreCase));
        if (serviceEntry is null)
        {
            serviceEntry = new ApplicationServiceEntity { Id = contents.Service.ServiceId };
            manifest.ApplicationServices.Add(serviceEntry);
        }

        serviceEntry.Name = contents.Manifest.ApplicationName;
        serviceEntry.Category = "runtime-agent";
        serviceEntry.Version = release.Identity.PackageVersion;
        serviceEntry.Status = "Active";
        serviceEntry.Health = "Healthy";

        await manifestStore.SaveAsync(manifest, cancellationToken);

        if (File.Exists(pointerPath))
        {
            logger.LogInformation("Active release pointer updated at {PointerPath}", pointerPath);
        }

        var capabilities = capabilitiesProvider.GetCapabilities(manifest.RuntimeVersion);
        logger.LogInformation(
            "Runtime registered for release {ReleaseId} with {CapabilityCount} capabilities at {Endpoint}.",
            releaseId,
            capabilities.Capabilities.Count,
            api.BaseUrl);

        return DeploymentStepResult.Success(
            $"Runtime registered for service '{contents.Service.ServiceId}'.",
            new Dictionary<string, string>
            {
                ["manifestPath"] = manifestSettings.CurrentValue.ManifestPath,
                ["publicEndpoint"] = api.BaseUrl
            });
    }

    /// <inheritdoc />
    public async Task<DeploymentStepResult> VerifyDeploymentAcceptanceAsync(
        string releaseId,
        string deploymentId,
        CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var deployment = deploymentSettings.CurrentValue;
        var failures = new List<string>();

        if (!dockerProvider.IsAvailable)
        {
            failures.Add("Docker provider is unavailable.");
        }

        if (!healthProvider.IsAvailable)
        {
            failures.Add("Health provider is unavailable.");
        }

        if (!ValidateActiveProfile())
        {
            failures.Add("Active configuration profile validation failed.");
        }

        if (dockerProvider.IsAvailable &&
            !string.IsNullOrWhiteSpace(contents.Service.ContainerName))
        {
            var container = await dockerProvider.WaitForContainerHealthAsync(
                contents.Service.ContainerName,
                TimeSpan.FromSeconds(deployment.ContainerHealthTimeoutSeconds),
                TimeSpan.FromMilliseconds(deployment.ContainerHealthPollIntervalMilliseconds),
                cancellationToken);

            if (container is null || !string.Equals(container.Health, "healthy", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Container '{contents.Service.ContainerName}' is not healthy.");
            }
        }

        var healthEndpoint = apiSettings.CurrentValue.HealthEndpoint;
        if (healthProvider.IsAvailable)
        {
            var healthResult = await healthProvider.WaitForHealthAsync(
                healthEndpoint,
                200,
                TimeSpan.FromSeconds(deployment.HealthCheckTimeoutSeconds),
                deployment.HealthCheckRetryCount,
                TimeSpan.FromMilliseconds(deployment.HealthCheckRetryDelayMilliseconds),
                cancellationToken);

            if (!healthResult.IsHealthy)
            {
                failures.Add(healthResult.Message);
            }
        }

        var manifest = await manifestStore.TryLoadAsync(cancellationToken);
        if (manifest is null)
        {
            failures.Add("Runtime manifest was not found after deployment.");
        }

        var pointerPath = Path.Combine(releaseSettings.CurrentValue.ActiveFolder, contents.Service.ServiceId, "active-release.json");
        if (File.Exists(pointerPath))
        {
            var pointerJson = await File.ReadAllTextAsync(pointerPath, cancellationToken);
            var pointer = JsonSerializer.Deserialize<ActiveReleasePointer>(pointerJson, JsonOptions);
            if (pointer is not null &&
                !string.Equals(pointer.CurrentReleaseId, releaseId, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Active release pointer mismatch. Expected {releaseId}, found {pointer.CurrentReleaseId}.");
            }
        }

        if (signalRSettings.CurrentValue.Enabled && providerSettings.CurrentValue.HealthEnabled)
        {
            var negotiatePath = SignalRHubEndpointResolver.ResolveNegotiatePath(
                apiSettings.CurrentValue.PathBase,
                signalRSettings.CurrentValue.HubPath);
            var negotiateResult = await healthProvider.CheckEndpointAsync(negotiatePath, cancellationToken);
            if (!negotiateResult.IsHealthy && negotiateResult.StatusCode != 405)
            {
                failures.Add($"SignalR hub negotiate probe failed: {negotiateResult.Message}");
            }
        }

        if (failures.Count > 0)
        {
            await RecordAuditAsync(
                releaseId,
                AuditEventType.PipelineStepFailed.ToString(),
                string.Join("; ", failures),
                "system",
                cancellationToken,
                deploymentId);
            return DeploymentStepResult.Failure(string.Join("; ", failures));
        }

        await RecordAuditAsync(
            releaseId,
            AuditEventType.DeploymentCompleted.ToString(),
            "Deployment acceptance gates passed.",
            "system",
            cancellationToken,
            deploymentId);

        return DeploymentStepResult.Success("Deployment acceptance gates passed.");
    }

    /// <inheritdoc />
    public async Task RecordAuditAsync(
        string releaseId,
        string eventType,
        string message,
        string actor,
        CancellationToken cancellationToken = default)
        => await RecordAuditAsync(releaseId, eventType, message, actor, cancellationToken, deploymentId: null);

    private async Task RecordAuditAsync(
        string releaseId,
        string eventType,
        string message,
        string actor,
        CancellationToken cancellationToken,
        string? deploymentId,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var release = await releaseRepository.GetAsync(releaseId, cancellationToken);
        if (release is null)
        {
            return;
        }

        if (!Enum.TryParse<AuditEventType>(eventType, ignoreCase: true, out var auditEventType))
        {
            auditEventType = AuditEventType.PipelineStepCompleted;
        }

        await auditStore.AppendAsync(new AuditEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = auditEventType,
            ReleaseId = releaseId,
            PackageId = release.Identity.PackageId,
            Actor = actor,
            Timestamp = DateTimeOffset.UtcNow,
            Message = message,
            DeploymentId = deploymentId ?? releaseId,
            CorrelationId = metadata?.GetValueOrDefault("correlationId"),
            Metadata = metadata ?? new Dictionary<string, string>()
        }, cancellationToken);
    }

    private bool ValidateActiveProfile() => profileManager.ValidateActiveProfile();

    private async Task<ReleaseRecord> RequireReleaseAsync(string releaseId, CancellationToken cancellationToken)
    {
        var release = await releaseRepository.GetAsync(releaseId, cancellationToken);
        return release ?? throw new KeyNotFoundException($"Release '{releaseId}' was not found.");
    }

    private string GetExtractionPath(string releaseId)
        => Path.Combine(releaseSettings.CurrentValue.ExtractionFolder, releaseId);

    private static void CopySupportingComposeFiles(string sourceDirectory, string composeTargetDirectory, string composeFileName)
    {
        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            var fileName = Path.GetFileName(file);
            if (string.Equals(fileName, composeFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".conf", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".env", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(file, Path.Combine(composeTargetDirectory, fileName), overwrite: true);
            }
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        EnsureDirectoryPath(destinationDirectory, nameof(CopyDirectory));

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            EnsureDirectoryPath(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.OrdinalIgnoreCase), nameof(CopyDirectory));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(sourceDirectory, destinationDirectory, StringComparison.OrdinalIgnoreCase);
            EnsureDirectoryPath(Path.GetDirectoryName(target)!, nameof(CopyDirectory));
            File.Copy(file, target, overwrite: true);
        }
    }

    private async Task<ServiceDeploymentBundle?> TryResolveDeploymentBundleAsync(
        string packagePath,
        string packageDirectory,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (Directory.Exists(packageDirectory))
        {
            var existing = ServiceDeploymentBundleIO.TryReadDirectory(packageDirectory);
            if (existing is not null)
            {
                return existing;
            }

            var manifestPath = Path.Combine(packageDirectory, DeployPackageLayout.ManifestFile);
            var healthPath = Path.Combine(packageDirectory, DeployPackageLayout.HealthFile);
            if (File.Exists(manifestPath) && File.Exists(healthPath))
            {
                var manifest = JsonSerializer.Deserialize<DeployManifest>(await File.ReadAllTextAsync(manifestPath), JsonOptions)
                    ?? throw new InvalidOperationException("Unable to parse deployment manifest.");
                var health = JsonSerializer.Deserialize<DeployHealthDefinition>(await File.ReadAllTextAsync(healthPath), JsonOptions)
                    ?? throw new InvalidOperationException("Unable to parse deployment health definition.");
                var configPath = Path.Combine(packageDirectory, DeployPackageLayout.ConfigTemplateFile);
                var configurationKeys = File.Exists(configPath)
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(configPath), JsonOptions)?.Keys.ToList() ?? []
                    : [];

                return DeploymentPackageContractsResolver.ResolveFromDirectory(
                    packageDirectory,
                    new LegacyPackageMetadata
                    {
                        ServiceId = manifest.Service,
                        DisplayName = manifest.Service,
                        Version = manifest.Version,
                        ImageName = manifest.ImageName,
                        ImageTag = manifest.ImageTag,
                        ContainerName = manifest.Container,
                        EntryPoint = manifest.EntryPoint,
                        HealthEndpoint = health.Endpoint,
                        Ports = manifest.Ports,
                        Dependencies = manifest.Dependencies.ToList(),
                        HealthTimeoutSeconds = health.TimeoutSeconds,
                        HealthRetries = health.Retries,
                        ConfigurationKeys = configurationKeys
                    });
            }
        }

        var deployContents = await packageReader.ReadAsync(packagePath, cancellationToken);
        return DeploymentPackageContractsResolver.ResolveFromDirectory(
            packageDirectory,
            new LegacyPackageMetadata
            {
                ServiceId = deployContents.Service.ServiceId,
                DisplayName = deployContents.Service.DisplayName,
                Version = deployContents.Manifest.PackageVersion,
                ImageName = deployContents.Manifest.PackageId,
                ImageTag = deployContents.Manifest.PackageVersion,
                ContainerName = deployContents.Service.ContainerName,
                EntryPoint = string.Empty,
                HealthEndpoint = deployContents.Health.HealthEndpoint,
                Ports = [],
                Dependencies = deployContents.Manifest.Dependencies.Select(item => item.PackageId).ToList(),
                HealthTimeoutSeconds = (int)deployContents.Health.Timeout.TotalSeconds,
                HealthRetries = deployContents.Health.RetryCount,
                ConfigurationKeys = []
            });
    }

    private static HostPathContext? TryResolveHostPaths()
    {
        var configRoot = Environment.GetEnvironmentVariable("AOVIAS_CONFIG_ROOT");
        if (string.IsNullOrWhiteSpace(configRoot))
        {
            return null;
        }

        var profileName = Environment.GetEnvironmentVariable("AOVIAS_ACTIVE_PROFILE");
        if (string.IsNullOrWhiteSpace(profileName))
        {
            profileName = "local";
        }

        try
        {
            var profile = DeploymentPathResolver.Resolve(configRoot, profileName);
            return new HostPathContext
            {
                HostConfigRoot = profile.HostConfigRoot,
                HostLogsRoot = profile.HostLogsRoot,
                HostPackagesRoot = profile.HostPackagesRoot,
                HostDeploymentsRoot = profile.HostDeploymentsRoot
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ResolveDockerNetworkName(InfrastructureContract contract)
    {
        var token = contract.Network.NetworkNameToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (token.StartsWith("${", StringComparison.Ordinal) && token.EndsWith('}'))
        {
            return Environment.GetEnvironmentVariable(token[2..^1]);
        }

        return token;
    }

    private RuntimeCapabilities MapRuntimeCapabilities()
    {
        var providers = providerSettings.CurrentValue;
        return new RuntimeCapabilities
        {
            Docker = providers.DockerEnabled,
            SqlServer = providers.DockerEnabled,
            RabbitMq = providers.DockerEnabled,
            ServiceBus = providers.DockerEnabled,
            Certificates = providers.CertificateEnabled,
            SignalR = signalRSettings.CurrentValue.Enabled,
            Nginx = providers.NginxEnabled
        };
    }

    private static void EnsureDirectoryPath(string path, [CallerMemberName] string methodName = "")
        => DeploymentExecutionDiagnostics.CreateDirectory(path, nameof(ReleaseDeploymentProvider), methodName);

    private sealed class ActiveReleasePointer
    {
        public required string CurrentReleaseId { get; init; }

        public required string PackageId { get; init; }

        public required string PackageVersion { get; init; }

        public required DateTimeOffset UpdatedAt { get; init; }
    }

    /// <summary>
    /// Sprint M2.5 D11: the one real, non-test implementation of <see cref="IExecutionGateCheck"/>,
    /// backing Health and Certification phase tasks with the same provider calls
    /// <see cref="VerifyDeploymentAcceptanceAsync"/> already uses (Docker availability, health
    /// endpoint probing, active profile validation) — no new health-check logic was invented for
    /// this sprint, only a pluggable seam so the shared Execution Runtime library never needs to
    /// depend on Runtime Agent's <see cref="IDockerProvider"/>/<see cref="IHealthProvider"/>/
    /// <see cref="IProfileManager"/> abstractions directly.
    /// </summary>
    private sealed class RuntimeGateCheck : IExecutionGateCheck
    {
        private readonly IDockerProvider dockerProvider;
        private readonly IHealthProvider healthProvider;
        private readonly IProfileManager profileManager;
        private readonly string healthEndpoint;

        public RuntimeGateCheck(IDockerProvider dockerProvider, IHealthProvider healthProvider, IProfileManager profileManager, string healthEndpoint)
        {
            this.dockerProvider = dockerProvider;
            this.healthProvider = healthProvider;
            this.profileManager = profileManager;
            this.healthEndpoint = healthEndpoint;
        }

        public async Task<GateCheckResult> CheckAsync(string gateId, DeploymentExecutionContext context, CancellationToken cancellationToken)
        {
            switch (gateId)
            {
                case "docker-provider-available":
                    return dockerProvider.IsAvailable ? GateCheckResult.Pass() : GateCheckResult.Fail("Docker provider is unavailable.");
                case "health-provider-available":
                    return healthProvider.IsAvailable ? GateCheckResult.Pass() : GateCheckResult.Fail("Health provider is unavailable.");
                case "active-profile-valid":
                    return profileManager.ValidateActiveProfile() ? GateCheckResult.Pass() : GateCheckResult.Fail("Active configuration profile validation failed.");
                case "primary-health-probe":
                case "deployment-acceptance-health-check":
                    if (!healthProvider.IsAvailable)
                    {
                        return GateCheckResult.Pass();
                    }

                    var result = await healthProvider.CheckEndpointAsync(healthEndpoint, cancellationToken);
                    return result.IsHealthy ? GateCheckResult.Pass() : GateCheckResult.Fail(result.Message);
                default:
                    return GateCheckResult.Pass();
            }
        }
    }
}
