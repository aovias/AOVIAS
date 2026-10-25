namespace Aovias.Bootstrap.Contracts;

/// <summary>
/// The target host's ability to run privileged (root) commands, detected once during
/// <see cref="BootstrapPhaseName.Preflight"/> and reused for every later phase that needs to run a
/// privileged command — never re-detected mid-run, since the target host does not change shape
/// during a single bootstrap attempt.
/// </summary>
public enum BootstrapPrivilegeMode
{
    /// <summary>The SSH user is already root — no elevation of any kind is needed.</summary>
    NotRequired,

    /// <summary><c>sudo -n</c> (no password) already succeeds for this user.</summary>
    Passwordless,

    /// <summary>The user can elevate (is in the sudo/wheel group) but <c>sudo -n</c> failed — a password is required for every privileged command until one is supplied.</summary>
    PasswordRequired,

    /// <summary>The user has no path to root at all (not root, not passwordless, not in the sudo/wheel group) — Preflight reports this as a blocker; Bootstrap cannot proceed.</summary>
    Unavailable
}

/// <summary>The operator's answer to one <see cref="IBootstrapElevationHandler.RequestSudoPasswordAsync"/> prompt.</summary>
public sealed class SudoPasswordPromptResult
{
    /// <summary>Null means the operator cancelled the prompt — the caller must treat this as a cancelled bootstrap run, not retry.</summary>
    public string? Password { get; init; }

    /// <summary>When true, the password is cached in memory (never on disk) for the remainder of this bootstrap run, so later privileged commands do not prompt again. When false, every subsequent privileged command re-prompts.</summary>
    public bool Remember { get; init; }

    public bool Cancelled => Password is null;
}

/// <summary>
/// The provider seam (Guideline #9) for asking the operator for a sudo password mid-run, when
/// <see cref="BootstrapPrivilegeMode.PasswordRequired"/> was detected at Preflight. Deliberately
/// request/response (unlike the fire-and-forget <see cref="IBootstrapExecutionObserver"/>) — the
/// orchestrator must actually wait for an answer before it can run the next privileged command.
/// No implementation may write the returned password to disk, a log, an exception message, or any
/// diagnostic/telemetry surface; the orchestrator holds it only in memory for the remainder of the
/// current <c>BootstrapAsync</c> call and discards it in a <c>finally</c> block regardless of
/// outcome (success, failure, or cancellation).
/// </summary>
public interface IBootstrapElevationHandler
{
    /// <param name="isRetryAfterRejection">True when the orchestrator is asking again because the
    /// previous password was rejected by sudo on the target host — lets the UI show "Authentication
    /// Failed" instead of the first-time prompt. False for every other ask (first time, or asking
    /// again later in the run because the operator chose not to have a prior password remembered).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SudoPasswordPromptResult> RequestSudoPasswordAsync(bool isRetryAfterRejection, CancellationToken cancellationToken);
}
