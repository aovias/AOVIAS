namespace Aovias.Bootstrap.Contracts;

/// <summary>
/// Sprint M4.1 — Zero-Touch Environment Bootstrap.
///
/// The fixed, ordered set of bootstrap phases. Like <c>DeploymentExecutionPhaseName</c>
/// (Sprint M2.4/M2.5, <c>Aovias.Deployment.Contracts.Planning.DeploymentPlan</c>), this is platform
/// policy, not something an operator or orchestrator can reorder or omit from: every phase after
/// the first depends on the ones before it actually having run (you cannot create the Docker
/// network before Docker is installed, cannot write runtime configuration before the config root
/// exists, cannot register an agent that has not started).
///
/// This is a deliberately separate enum from <c>DeploymentExecutionPhaseName</c> — that enum
/// describes how an already-bootstrapped Runtime Agent executes a service deployment; this one
/// describes how a bare VPS becomes a Runtime-Agent-hosting environment in the first place. They
/// are different lifecycles applied to different subjects (a service release vs. a whole
/// environment) and neither is a special case of the other.
/// </summary>
public enum BootstrapPhaseName
{
    /// <summary>
    /// Sprint M7.x.2 — SSH connectivity, promoted to its own first-class stage. Previously this ran
    /// as unguarded code before the stage loop even started; an exception there had no
    /// <see cref="BootstrapPhaseName"/> to attach to, so the generic Operation Engine's safety net
    /// could only report a bare, unattributed failure — the reported "reports Failed at Connect
    /// while the actual exception originates elsewhere" defect traces directly to that gap. As a
    /// real stage it flows through the exact same guaranteed-attribution lifecycle as every other
    /// phase (see <c>BootstrapOrchestrator.RunStageAsync</c>).
    /// </summary>
    Connect = 1,

    /// <summary>
    /// Sprint M7.x.2 — privilege-mode detection (root / passwordless sudo / password sudo /
    /// unavailable), promoted to its own stage for the same reason as <see cref="Connect"/>. Also
    /// closes a related latent defect: detection previously lived inside the old combined
    /// Preflight-phase's pre-loop code, so a *resumed* run whose Preflight was checkpoint-skipped
    /// never re-detected the mode — leaving it defaulted to "no elevation" and silently running
    /// every later privileged command unelevated. This stage's discovery never honors the
    /// checkpoint (privilege mode is a session property, not a one-time fact) — it always re-runs.
    /// </summary>
    ValidatePrivileges = 2,

    /// <summary>Host prerequisite checks against the target (OS/Ubuntu release, CPU architecture, disk, memory, outbound network, DNS resolution, registration-intent). No changes made.</summary>
    Preflight = 3,

    /// <summary>Verifies Docker is installed and running; installs it if (and only if) absent.</summary>
    DockerInstallation = 4,

    /// <summary>Verifies the platform's Docker network exists; creates it if absent. Idempotent — never recreated if already present.</summary>
    DockerNetworkCreation = 5,

    /// <summary>Creates the minimal remote directory scaffold (the config-root parent and the active profile directory) that Runtime Agent's own startup validation needs to find something valid. Does not attempt to create Runtime Agent's own nested runtime directories (packages/logs/deployments/etc.) — <c>IDeploymentPathResolver.ValidateStorageLayout(createMissing:true)</c> already does that, every time the agent starts. Duplicating that here would be exactly the parallel implementation Guideline #3 forbids.</summary>
    RuntimeDirectoryCreation = 6,

    /// <summary>Generates and uploads every <c>settings.*.json</c> file the active profile needs (see <c>RuntimeConfigurationScaffoldBuilder.BuildRequiredProfileSettings</c>) — no new configuration schema, only complete coverage of the existing one.</summary>
    RuntimeConfigurationGeneration = 7,

    /// <summary>
    /// Defect correction (Bootstrap Configuration Validation &amp; Runtime Startup Diagnostics):
    /// validates every file <see cref="RuntimeConfigurationGeneration"/> just uploaded against its
    /// corresponding <c>Aovias.Configuration.Settings</c> class's declared requirements — before
    /// Docker is ever started — so a missing or empty required property (e.g. logging's
    /// <c>logRoot</c>, the exact real-world defect this phase exists to catch) is Bootstrap's own
    /// blocker instead of a Runtime Agent crash discovered four phases later at registration.
    /// </summary>
    ValidateRuntimeConfiguration = 8,

    /// <summary>Uploads the Runtime Agent artifact, starts it, and waits for it to actually be running and healthy (container status + Docker healthcheck) before this phase reports success — so <see cref="RuntimeAgentRegistration"/> never runs against a crash-looping container.</summary>
    RuntimeAgentInstallation = 9,

    /// <summary>Registers the newly-started agent with itself (token-validated self-registration, issuing an API key) and then with the Command Center's Fleet Registry (so it is monitored) — reusing the two existing registration endpoints rather than inventing a third.</summary>
    RuntimeAgentRegistration = 10,

    /// <summary>Confirms the environment is actually reachable and healthy end-to-end via the agent's own heartbeat endpoint.</summary>
    BootstrapVerification = 11
}

/// <summary>Which stage lifecycle step (see <c>Aovias.Bootstrap.Core.Stages.BootstrapStage</c>) an exception was actually thrown from — carried on <see cref="BootstrapStageException"/> and used to build a precise failure message rather than a generic one.</summary>
public enum BootstrapStageLifecycleStep
{
    Discover,
    Validate,
    Execute,
    Verify,
    Repair,
    CollectDiagnostics
}

/// <summary>
/// Sprint M7.x.2 — the structured exception every stage lifecycle boundary converts an unexpected
/// failure into, immediately at the point it was caught (never after passing through additional
/// call frames that could lose or blur which stage/step it came from). Carrying <see cref="Phase"/>
/// and <see cref="LifecycleStep"/> explicitly is what guarantees the UI always reports the actual
/// stage that failed — the direct fix for "Bootstrap reports Failed at Connect while the actual
/// exception originates from DockerNetworkCreation": that defect was possible only because the old
/// pre-loop session-init code ran outside any stage's lifecycle boundary, so an exception there had
/// no phase to attach to at all. Every lifecycle call now happens inside a guard that constructs
/// this immediately on catch — there is no remaining code path where a raw exception can propagate
/// past a stage boundary unattributed.
/// </summary>
public sealed class BootstrapStageException(BootstrapPhaseName phase, BootstrapStageLifecycleStep lifecycleStep, Exception inner)
    : Exception($"Stage '{phase}' threw during {lifecycleStep}: {inner.Message}", inner)
{
    public BootstrapPhaseName Phase { get; } = phase;

    public BootstrapStageLifecycleStep LifecycleStep { get; } = lifecycleStep;
}

/// <summary>The result of attempting a single bootstrap phase.</summary>
public enum BootstrapPhaseOutcome
{
    /// <summary>The phase ran and made the change it was responsible for.</summary>
    Completed,

    /// <summary>The phase found its target condition already satisfied and made no change — the mechanism idempotence and resumability are built on.</summary>
    AlreadySatisfied,

    /// <summary>The phase could not complete; see the result's <c>RootCause</c>.</summary>
    Failed
}

/// <summary>
/// Everything needed to reach and act on one target VPS. <see cref="PrivateKeyPath"/> and
/// <see cref="Password"/> are both optional but exactly one must be supplied — see
/// <see cref="BootstrapRequest"/> validation. Nothing here is persisted with the phase-completion
/// state file (<c>BootstrapStateStore</c>): only <see cref="StableIdentity"/> is, so credentials
/// are never written to disk by the orchestrator itself.
/// </summary>
public sealed class BootstrapTarget
{
    public required string Host { get; init; }

    public int Port { get; init; } = 22;

    public required string Username { get; init; }

    public string? PrivateKeyPath { get; init; }

    public string? Password { get; init; }

    /// <summary>Deployment profile name this environment will run under (for example <c>demo</c>, <c>uat</c>, <c>customer-a</c>).</summary>
    public required string ProfileName { get; init; }

    /// <summary>Display name passed through to <c>AgentRegistrationRequest.EnvironmentName</c> and <c>FleetRegistrationRequest.Name</c>.</summary>
    public required string EnvironmentName { get; init; }

    public Aovias.Release.Contracts.RemoteDeployment.EnvironmentType EnvironmentType { get; init; }

    /// <summary>The pre-shared token the target agent's own registration endpoint validates (see <c>EnvironmentRegistrationStore.SeedDefaultTokens</c>). Zero-touch bootstrap does not — and, per this sprint's "no new deployment features" rule, must not — mint new tokens; it can only use one the platform already recognizes.</summary>
    public required string RegistrationToken { get; init; }

    /// <summary>The public base URL the new agent will be reachable at once running (used both as <c>AgentRegistrationRequest.PublicUrl</c> and to call the agent directly during registration/verification).</summary>
    public required string PublicUrl { get; init; }

    public required string CommandCenterBaseUrl { get; init; }

    /// <summary>Local path to the Runtime Agent publish output to install on the target host.</summary>
    public required string RuntimeAgentArtifactPath { get; init; }

    /// <summary>
    /// Version of <see cref="RuntimeAgentArtifactPath"/> from the Release Library selection.
    /// When set, Runtime Agent installation discovery compares this against the version already
    /// deployed on the target so a newer published agent is installed even when the container is healthy.
    /// </summary>
    public string? RuntimeAgentArtifactVersion { get; init; }

    /// <summary>Remote config root (container-side, e.g. <c>/opt/aovias/config</c>) this environment's profile will live under.</summary>
    public required string RemoteConfigRoot { get; init; }

    public required string DockerNetworkName { get; init; }

    /// <summary>A stable, non-secret identity for this target used as the resumable-state key: which host/port/profile, not who/how it is reached.</summary>
    public string StableIdentity => $"{Host}:{Port}:{ProfileName}";
}

/// <summary>A bootstrap run request. <see cref="Force"/> re-runs every phase even if the state store says it already completed for this target — the escape hatch for "start over," never the default.</summary>
public sealed class BootstrapRequest
{
    public required BootstrapTarget Target { get; init; }

    public bool Force { get; init; }

    /// <summary>Where the state file and generated <see cref="BootstrapReport"/> are written locally (on the machine running the bootstrap tool, not the target).</summary>
    public required string WorkingDirectory { get; init; }
}

/// <summary>The outcome of one phase, always populated whether the phase ran, was skipped, or failed.</summary>
public sealed record BootstrapPhaseResult
{
    public required BootstrapPhaseName Phase { get; init; }

    public required BootstrapPhaseOutcome Outcome { get; init; }

    public required string Message { get; init; }

    public string? RootCause { get; init; }

    public DateTimeOffset CompletedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Sprint M7.x.2 Stage Timeline field. When this phase started — populated by the orchestrator, distinct from <see cref="CompletedUtc"/> so a UI can render actual duration.</summary>
    public DateTimeOffset? StartedUtc { get; init; }

    /// <summary>Sprint M7.x.2 Stage Timeline field. How many Execute/Verify retry cycles ran after a successful automatic repair (0 for a stage that succeeded, or failed, on its first attempt).</summary>
    public int RetryCount { get; init; }

    /// <summary>Sprint M7.x.2 Stage Timeline field. One entry per automatic repair attempt this phase made, in order, describing what was tried and whether it succeeded — the same text already folded into a failure's <see cref="RootCause"/>, exposed separately so a UI can render it as its own timeline column instead of only inside the failure card.</summary>
    public IReadOnlyList<string> RepairAttempts { get; init; } = [];

    /// <summary>Sprint M7.x.2 Stage Timeline field. The post-execution verification's own evidence/result text — populated on both success and failure so the timeline can show "what verification actually found," not just pass/fail.</summary>
    public string? VerificationResult { get; init; }

    public bool Succeeded => Outcome is BootstrapPhaseOutcome.Completed or BootstrapPhaseOutcome.AlreadySatisfied;
}

/// <summary>
/// The single bootstrap report produced per run (one per target, per attempt) — this sprint's
/// "single Bootstrap report" deliverable. Every bootstrap phase this platform ever gains belongs on
/// this same report; there is deliberately no per-phase or per-sprint parallel report type, the same
/// discipline <c>PlatformVerificationReport</c> (Sprint M3.3/M3.4) established for publish/deploy
/// verification.
/// </summary>
public sealed record BootstrapReport
{
    public required string TargetIdentity { get; init; }

    public required IReadOnlyList<BootstrapPhaseResult> Phases { get; init; }

    public bool OverallSuccess => Phases.Count > 0 && Phases.All(phase => phase.Succeeded);

    /// <summary>The first phase that failed, if any — the deterministic root cause, since phases run in fixed order and a failure stops the run.</summary>
    public BootstrapPhaseResult? FirstFailure => Phases.FirstOrDefault(phase => !phase.Succeeded);

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? ReportPath { get; init; }
}
