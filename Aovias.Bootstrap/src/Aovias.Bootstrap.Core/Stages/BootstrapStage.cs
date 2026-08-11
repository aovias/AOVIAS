using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>Everything a stage needs to act on the target — services, tunables, and the per-stage checkpoint hint.</summary>
public sealed class BootstrapStageContext
{
    public required BootstrapTarget Target { get; init; }

    public required RemoteExecutionEngine Engine { get; init; }

    public required IBootstrapTargetExecutor Executor { get; init; }

    public required IBootstrapRegistrationClient RegistrationClient { get; init; }

    public required PermissionRepairService Permissions { get; init; }

    public required DockerManager Docker { get; init; }

    public required ConfigurationSynchronizer Configuration { get; init; }

    public required BootstrapTunables Tunables { get; init; }

    public IBootstrapExecutionObserver? Observer { get; init; }

    /// <summary>
    /// Whether the local state store recorded this stage complete on a previous run. A HINT, never
    /// authority: remote state discovered by <see cref="BootstrapStage.DiscoverAsync"/> always wins
    /// (Sprint M7.x: "Never depend on local completion state alone. Remote state is authoritative.").
    /// </summary>
    public bool Checkpointed { get; set; }

    /// <summary>
    /// The API key the agent's self-registration issued THIS run — set by <see cref="RegistrationStage"/>,
    /// consumed by <see cref="VerificationStage"/>'s heartbeat polling (the heartbeat endpoint sits
    /// behind the agent's <c>ApiKeyAuthenticationMiddleware</c>; an anonymous poll gets 401 —
    /// real-VPS defect). In memory only, for the lifetime of one bootstrap run — never persisted,
    /// matching the platform rule that registration secrets are never written to disk. Registration
    /// re-runs on every attempt precisely so this key always exists by the time verification needs it.
    /// </summary>
    public string? IssuedApiKey { get; set; }
}

/// <summary>Polling/retry knobs, grouped so stage signatures stay stable as knobs are added.</summary>
public sealed record BootstrapTunables(
    int HealthVerificationMaxAttempts,
    int HealthVerificationPollDelayMs,
    int RegistrationMaxAttempts,
    int RegistrationRetryDelayMs,
    int ContainerHealthMaxAttempts,
    int ContainerHealthPollDelayMs,
    int ConnectRetryDelayMs = 2000);

/// <summary>Pre-execution remote-state discovery: is this stage's target condition already genuinely met on the host?</summary>
public sealed record StageDiscovery(bool Satisfied, string Evidence)
{
    public static StageDiscovery SatisfiedBecause(string evidence) => new(true, evidence);

    public static StageDiscovery NotSatisfied(string evidence) => new(false, evidence);
}

/// <summary>Precondition validation — pure checks of intent and environment that must abort the stage before it makes any change.</summary>
public sealed record StageValidation(bool Valid, string? Blockers = null)
{
    public static readonly StageValidation Ok = new(true);

    public static StageValidation Blocked(string blockers) => new(false, blockers);
}

/// <summary>The outcome of a stage's Execute or Verify step. On failure, <see cref="Message"/> carries the full classified failure headline (e.g. "Container Crash — ...") and <see cref="Detail"/> the root-cause detail.</summary>
public sealed record StageOutcome(bool Succeeded, string Message, string? Detail = null)
{
    public static StageOutcome Ok(string message) => new(true, message);

    public static StageOutcome Fail(string message, string? detail = null) => new(false, message, detail);
}

/// <summary>One automatic repair attempt's record — surfaced verbatim in the failure diagnostics so the operator sees what self-healing was tried and how it went.</summary>
public sealed record StageRepair(bool Attempted, bool Succeeded, string Description)
{
    public static readonly StageRepair None = new(false, false, "No automatic repair applies to this failure.");

    public static StageRepair Repaired(string description) => new(true, true, description);

    public static StageRepair Failed(string description) => new(true, false, description);
}

/// <summary>
/// Sprint M7.x — one stage of the bootstrap orchestration pipeline. Each stage owns its own
/// lifecycle: precondition validation, remote-state discovery (authoritative over local
/// checkpoints), execution, post-execution verification, bounded automatic repair, diagnostics
/// collection, and (conservative) rollback. The orchestrator drives the lifecycle; stages never
/// sequence each other.
/// </summary>
public abstract class BootstrapStage
{
    public abstract BootstrapPhaseName Phase { get; }

    /// <summary>Preconditions — pure checks that must block execution before any change is made. Default: no preconditions beyond what discovery/execution themselves enforce.</summary>
    public virtual Task<StageValidation> ValidateAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageValidation.Ok);

    /// <summary>
    /// Remote-state discovery, run before every execution: inspect the actual host and report
    /// whether this stage's condition is already genuinely met. The default says "never satisfied"
    /// — correct for stages that must re-run every time (in-process validation, end-to-end health
    /// verification); stages with a readable remote condition override this.
    /// </summary>
    public virtual Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageDiscovery.NotSatisfied("This stage re-runs on every attempt."));

    public abstract Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Post-execution verification — proves the change actually took on the host, not merely that
    /// commands exited 0. The default re-runs <see cref="DiscoverAsync"/> and requires it to report
    /// satisfied; stages whose discovery deliberately never reports satisfied MUST override this.
    /// </summary>
    public virtual async Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var discovery = await DiscoverAsync(context, cancellationToken);
        return discovery.Satisfied
            ? StageOutcome.Ok(discovery.Evidence)
            : StageOutcome.Fail($"{Phase} executed but post-execution verification did not confirm the result.", discovery.Evidence);
    }

    /// <summary>
    /// Bounded automatic repair, attempted once per failure site before the stage fails
    /// (Sprint M7.x "only fail after recovery attempts"). <paramref name="executionFailed"/>
    /// distinguishes an Execute failure (repair, then one re-Execute) from a Verify failure
    /// (repair, then one re-Verify — never a full re-Execute). Default: nothing to repair.
    /// </summary>
    public virtual Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
        => Task.FromResult(StageRepair.None);

    /// <summary>Evidence collection on failure — appended to the failure's root cause so the operator never sees a bare error. Default: nothing beyond the failure detail itself.</summary>
    public virtual Task<string?> CollectDiagnosticsAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    /// <summary>
    /// Rollback is exposed per the pipeline contract but deliberately conservative by default:
    /// bootstrap stages install and configure — they never own user data, and destructive
    /// un-installation on failure (removing Docker, deleting directories that may hold operator
    /// content) would be worse than leaving a diagnosable partial state for the next resumable
    /// attempt. Stages override only where a genuinely safe inverse exists.
    /// </summary>
    public virtual Task RollbackAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
