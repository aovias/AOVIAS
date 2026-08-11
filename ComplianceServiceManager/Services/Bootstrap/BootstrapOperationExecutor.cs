using Aovias.Bootstrap.Contracts;
using Aovias.Bootstrap.Execution;
using ComplianceServiceManager.Operations;

namespace ComplianceServiceManager.Services.Bootstrap;

/// <summary>
/// Adapts a bootstrap run onto the generic Operation Engine. Runs <see cref="BootstrapRunner"/> —
/// the same composition root the CLI itself now calls (see Phase A) — in-process, translating its
/// real <see cref="IBootstrapExecutionObserver"/> callbacks into <see cref="OperationContext"/>
/// calls. There is no separate provisioning logic here: this class only maps events, it never
/// decides what "installing Docker" or "verifying health" means.
/// </summary>
public sealed class BootstrapOperationExecutor : IOperationExecutor
{
    /// <summary>1:1 with <see cref="BootstrapPhaseName"/> — the timeline shows exactly the phases
    /// the engine actually runs, never a cosmetic invented stage. Weights are a rough relative-cost
    /// estimate (agent upload/install is by far the heaviest phase); see
    /// <c>OperationHistoryStore</c> remarks for how these could be tuned from real run timings later.</summary>
    private static readonly (BootstrapPhaseName Phase, string Name, string Description, double Weight)[] StageDefinitions =
    [
        (BootstrapPhaseName.Connect, "Connect", "Opens an SSH connection and verifies the target is reachable.", 1),
        (BootstrapPhaseName.ValidatePrivileges, "Validate Privileges", "Detects the elevation path (root, passwordless sudo, or password sudo) this session will use.", 1),
        (BootstrapPhaseName.Preflight, "Verify Host Requirements", "Confirms the target is a supported Ubuntu release with sufficient disk/memory and working network/DNS.", 1),
        (BootstrapPhaseName.DockerInstallation, "Verify Ubuntu & Install Docker", "Confirms the target is Ubuntu and installs Docker Engine if not already present.", 2),
        (BootstrapPhaseName.DockerNetworkCreation, "Create Docker Network", "Creates the platform Docker network.", 1),
        (BootstrapPhaseName.RuntimeDirectoryCreation, "Create Platform Directories", "Prepares the runtime directory layout through the permission repair engine (ownership, attributes, container access).", 1),
        (BootstrapPhaseName.RuntimeConfigurationGeneration, "Generate Configuration", "Generates the full Runtime Agent configuration and synchronizes it by checksum — only missing or drifted files are uploaded.", 1),
        (BootstrapPhaseName.ValidateRuntimeConfiguration, "Validate Runtime Configuration", "Validates every required configuration file and property before Docker is ever started.", 1),
        (BootstrapPhaseName.RuntimeAgentInstallation, "Runtime Startup", "Uploads the Runtime Agent, starts it via Docker Compose, and waits until it is running and healthy — self-healing (permission repair + recreate) before failing.", 4),
        (BootstrapPhaseName.RuntimeAgentRegistration, "Register Environment", "Registers the agent with itself and the Command Center.", 1),
        (BootstrapPhaseName.BootstrapVerification, "Verify Health", "Polls the agent heartbeat until the environment reports healthy.", 1)
    ];

    private readonly BootstrapRunner runner;
    private readonly BootstrapTarget target;
    private readonly bool force;
    private readonly string workingDirectory;
    private readonly IBootstrapElevationHandler? elevationHandler;

    public BootstrapOperationExecutor(BootstrapRunner runner, BootstrapTarget target, bool force, string workingDirectory, IBootstrapElevationHandler? elevationHandler = null)
    {
        this.runner = runner;
        this.target = target;
        this.force = force;
        this.workingDirectory = workingDirectory;
        this.elevationHandler = elevationHandler;

        Descriptor = new OperationDescriptor
        {
            Kind = "Bootstrap",
            DisplayName = $"Bootstrap {target.EnvironmentName} ({target.Host})",
            TargetIdentity = target.StableIdentity,
            SupportsResume = true,
            SupportsRetry = true,
            SupportsCancel = true,
            Stages = StageDefinitions
                .Select(definition => new OperationStageDefinition
                {
                    Id = definition.Phase.ToString(),
                    Name = definition.Name,
                    Description = definition.Description,
                    Weight = definition.Weight
                })
                .ToList()
        };
    }

    public OperationDescriptor Descriptor { get; }

    public Task ExecuteAsync(OperationContext context)
    {
        var observer = new BootstrapObserverAdapter(context);
        return runner.RunAsync(target, force, workingDirectory, observer, loggerFactory: null, context.CancellationToken, elevationHandler);
    }

    private static string FriendlyPhaseName(BootstrapPhaseName phase)
        => StageDefinitions.FirstOrDefault(definition => definition.Phase == phase).Name ?? phase.ToString();

    /// <summary>
    /// Error classification (Bootstrap Configuration Validation &amp; Runtime Startup Diagnostics
    /// defect fix): <c>BootstrapOrchestrator</c>'s own <c>Failed(...)</c> messages for every phase
    /// this fix touches now start with one of five category labels — "Configuration Validation
    /// Failed", "Runtime Startup Failed", "Container Crash", "Health Check Failed", "Registration
    /// Failed" — so the category recognized here is exactly what Core decided, not a second
    /// independent guess re-derived from keyword sniffing.
    /// </summary>
    private static readonly string[] KnownFailureCategories =
    [
        "Configuration Validation Failed",
        "Container Crash",
        "Docker Network Failed",
        "Health Check Failed",
        "Registration Failed",
        "Runtime Startup Failed"
    ];

    private static string BuildProblem(BootstrapPhaseResult result)
    {
        var category = KnownFailureCategories.FirstOrDefault(c => result.Message.Contains(c, StringComparison.Ordinal));
        return category ?? $"{FriendlyPhaseName(result.Phase)} failed.";
    }

    private static string BuildRecommendedAction(string message)
    {
        if (message.Contains("Configuration Validation Failed", StringComparison.Ordinal))
        {
            return "Regenerate Runtime Configuration, review the affected file(s) in Open Configuration, then Retry Bootstrap.";
        }

        if (message.Contains("Container Crash", StringComparison.Ordinal))
        {
            return "The Runtime Agent container is repeatedly restarting — review the attached docker logs/inspect diagnostics below for the actual crash cause, resolve it, then Retry Bootstrap.";
        }

        if (message.Contains("Docker Network Failed", StringComparison.Ordinal))
        {
            return "Review the attached Docker diagnostics below (daemon status, existing networks, socket permissions, rootless detection) for the actual conflict or permission cause, resolve it, then Retry Bootstrap.";
        }

        if (message.Contains("Health Check Failed", StringComparison.Ordinal) || message.Contains("heartbeat", StringComparison.OrdinalIgnoreCase))
        {
            return "The Runtime Agent did not report healthy in time. Review the attached diagnostics below, check the agent's own logs on the target host, then retry.";
        }

        if (message.Contains("Registration Failed", StringComparison.Ordinal))
        {
            return "Verify the registration token and Command Center URL are correct and the environment is reachable, then retry.";
        }

        if (message.Contains("Runtime Startup Failed", StringComparison.Ordinal) || message.Contains("docker", StringComparison.OrdinalIgnoreCase))
        {
            return "Review the attached diagnostics below (docker logs / inspect / compose ps), resolve the underlying issue on the target host, then retry.";
        }

        if (message.Contains("connect", StringComparison.OrdinalIgnoreCase) || message.Contains("ssh", StringComparison.OrdinalIgnoreCase))
        {
            return "Verify the target host is reachable and the SSH credentials are still valid, then retry.";
        }

        return "Review the details below, resolve the underlying issue on the target host, then retry — completed steps will not be repeated.";
    }

    private static string FormatBytes(ulong bytes)
    {
        double size = bytes;
        string[] units = ["B", "KB", "MB", "GB"];
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }

    /// <summary>Translates <see cref="IBootstrapExecutionObserver"/> callbacks into
    /// <see cref="OperationContext"/> calls. Tracks "current stage" itself because command/upload/log
    /// events don't carry a phase — see <see cref="IBootstrapExecutionObserver"/>'s remarks on why
    /// that's safe (phases run strictly sequentially).</summary>
    private sealed class BootstrapObserverAdapter : IBootstrapExecutionObserver
    {
        private readonly OperationContext context;

        // Sprint M7.x.2: connectivity and privilege detection are now real first-class stages
        // (Connect, ValidatePrivileges) that always run first and call OnPhaseStarting themselves,
        // so this default is only a defensive fallback for any stray callback that could somehow
        // arrive before the very first OnPhaseStarting — an empty stage id here previously made
        // OperationContext throw ("Unknown stage id ''"), which crashed the executor's observer
        // callback and surfaced as a bogus, misattributed failure.
        private string currentStageId = BootstrapPhaseName.Connect.ToString();

        public BootstrapObserverAdapter(OperationContext context)
        {
            this.context = context;
        }

        public void OnPhaseStarting(BootstrapPhaseName phase)
        {
            currentStageId = phase.ToString();
            context.BeginStage(currentStageId);
        }

        public void OnPhaseFinished(BootstrapPhaseResult result)
        {
            var stageId = result.Phase.ToString();
            if (result.Outcome == BootstrapPhaseOutcome.Failed)
            {
                context.FailStage(stageId, BuildFailure(result));
                return;
            }

            if (result.Outcome == BootstrapPhaseOutcome.AlreadySatisfied)
            {
                context.Log(stageId, result.Message);
            }

            context.CompleteStage(stageId, OperationStageStatus.Completed);
        }

        public void OnCommandStarting(string command) => context.Log(currentStageId, $"$ {command}");

        public void OnCommandFinished(string command, RemoteCommandResult result)
        {
            if (!result.Succeeded)
            {
                context.Log(currentStageId, $"exit {result.ExitCode}: {result.StandardError}", OperationLogSeverity.Warning);
            }
        }

        public void OnUploadProgress(string remotePath, ulong bytesTransferred, long totalBytes)
        {
            var fraction = totalBytes > 0 ? (double)bytesTransferred / totalBytes : 0;
            var fileName = remotePath.Contains('/') ? remotePath[(remotePath.LastIndexOf('/') + 1)..] : remotePath;
            context.ReportTask(
                currentStageId,
                $"Uploading {fileName} — {FormatBytes(bytesTransferred)} / {FormatBytes((ulong)Math.Max(0, totalBytes))}",
                fraction);
        }

        public void OnLog(string message) => context.Log(currentStageId, message);

        public void OnPhaseProgress(BootstrapPhaseName phase, string message, double? fraction)
        {
            var stageId = phase.ToString();
            context.ReportTask(stageId, message, fraction);
            context.Log(stageId, message);
        }

        private static OperationFailure BuildFailure(BootstrapPhaseResult result)
        {
            var problem = BuildProblem(result);

            // Core's message is "<Category> — <specifics>." — the card's own title already shows the
            // category, so Cause shows just the specifics instead of repeating it.
            var cause = problem != result.Message && result.Message.Contains('—')
                ? result.Message[(result.Message.IndexOf('—') + 1)..].Trim()
                : result.Message;

            return new OperationFailure
            {
                Problem = problem,
                Cause = cause,
                RecommendedAction = BuildRecommendedAction(result.Message),
                // Automatic diagnostics (docker logs/inspect/compose ps for Runtime Startup failures,
                // or the full File/Property/Expected/Actual/SuggestedFix breakdown for a configuration
                // validation failure) live in RootCause — never collapse this away, per "never display
                // only 'Connection failed'."
                Details = string.IsNullOrWhiteSpace(result.RootCause) ? result.Message : $"{result.Message}{Environment.NewLine}{Environment.NewLine}{result.RootCause}"
            };
        }
    }
}
