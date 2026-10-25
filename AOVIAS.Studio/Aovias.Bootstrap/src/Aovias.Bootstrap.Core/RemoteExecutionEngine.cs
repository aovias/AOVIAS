using System.Diagnostics;
using Aovias.Bootstrap.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.Bootstrap.Core;

/// <summary>One executed remote command, as recorded in the session transcript — command text only, never a sudo password (passwords travel via <c>ExecuteElevatedCommandAsync</c>'s dedicated parameter and are never part of the command string).</summary>
public sealed record ExecutedCommandRecord(
    DateTimeOffset StartedUtc,
    string Command,
    bool Elevated,
    int ExitCode,
    long DurationMs,
    string StandardErrorFirstLine);

/// <summary>
/// Sprint M7.x — the Remote Execution Engine. The single seam every remote command (privileged or
/// not) runs through: it owns the privilege-elevation strategy (mode detection, sudo password
/// prompting/caching, wrong-password re-prompt), per-command timing, a session transcript for
/// exportable diagnostics, and transient-failure recovery (an SSH drop mid-run is retried after a
/// reconnect attempt rather than failing the whole bootstrap).
///
/// This replaces <c>BootstrapOrchestrator.RunPrivilegedCommandAsync</c> and, critically, moves
/// privilege detection out of the Preflight *phase*: detection now happens at session start on
/// every run (see <c>BootstrapOrchestrator</c>'s session initialization), because a resumed run
/// whose Preflight was checkpoint-skipped previously never re-detected the mode — leaving it at
/// <see cref="BootstrapPrivilegeMode.NotRequired"/> and silently running every "privileged" command
/// unelevated. That was a real latent defect of the phase-owned design.
/// </summary>
public sealed class RemoteExecutionEngine(
    IBootstrapTargetExecutor executor,
    IBootstrapElevationHandler? elevationHandler,
    ILogger logger,
    int transientRetryAttempts = 2,
    int transientRetryDelayMs = 1000)
{
    private readonly List<ExecutedCommandRecord> transcript = [];

    /// <summary>In-memory only, for the lifetime of one bootstrap session — never written to disk, a log, an exception, the transcript, or any settings/telemetry surface. Cleared via <see cref="ClearSensitiveState"/> regardless of outcome.</summary>
    private string? cachedSudoPassword;

    /// <summary>Detected once per session (every session, including resumed ones) via <see cref="DetectPrivilegeModeAsync"/>.</summary>
    public BootstrapPrivilegeMode PrivilegeMode { get; private set; } = BootstrapPrivilegeMode.NotRequired;

    /// <summary>Human-readable explanation of how <see cref="PrivilegeMode"/> was determined — surfaced by Preflight as evidence.</summary>
    public string PrivilegeModeDetail { get; private set; } = "Privilege mode has not been detected yet.";

    /// <summary>Every command executed this session, in order — the raw material for the persisted session transcript.</summary>
    public IReadOnlyList<ExecutedCommandRecord> Transcript => transcript;

    /// <summary>
    /// Never fails the run just because a password is required — that is one of three legitimate,
    /// supported modes (see <see cref="BootstrapPrivilegeMode"/>), not a blocker. Only
    /// <see cref="BootstrapPrivilegeMode.Unavailable"/> (no path to root at all) is a blocker, and
    /// even that is reported as a normal Preflight blocker rather than a special-cased failure.
    /// </summary>
    public async Task DetectPrivilegeModeAsync(BootstrapTarget target, CancellationToken cancellationToken)
    {
        if (string.Equals(target.Username, "root", StringComparison.Ordinal))
        {
            PrivilegeMode = BootstrapPrivilegeMode.NotRequired;
            PrivilegeModeDetail = "Connected as root — no elevation needed.";
            return;
        }

        var passwordless = await RunAsync("sudo -n true", cancellationToken);
        if (passwordless.Succeeded)
        {
            PrivilegeMode = BootstrapPrivilegeMode.Passwordless;
            PrivilegeModeDetail = $"Passwordless sudo is available for '{target.Username}'.";
            return;
        }

        var groups = await RunAsync("id -nG", cancellationToken);
        var canElevate = groups.Succeeded
            && (groups.StandardOutput.Contains("sudo", StringComparison.OrdinalIgnoreCase) || groups.StandardOutput.Contains("wheel", StringComparison.OrdinalIgnoreCase));

        if (canElevate)
        {
            PrivilegeMode = BootstrapPrivilegeMode.PasswordRequired;
            PrivilegeModeDetail = $"'{target.Username}' can elevate via sudo, but a password is required.";
        }
        else
        {
            PrivilegeMode = BootstrapPrivilegeMode.Unavailable;
            PrivilegeModeDetail = $"user '{target.Username}' has no path to root — not in the sudo/wheel group, and passwordless sudo is unavailable.";
        }
    }

    /// <summary>Runs an unprivileged command with transient-failure recovery and transcript recording.</summary>
    public Task<RemoteCommandResult> RunAsync(string command, CancellationToken cancellationToken)
        => ExecuteWithRecoveryAsync(() => executor.ExecuteCommandAsync(command, cancellationToken), command, elevated: false, cancellationToken);

    /// <summary>
    /// Runs a root-requiring command using whichever elevation strategy <see cref="PrivilegeMode"/>
    /// dictates: no elevation for an already-root user, <c>sudo -n</c> for passwordless sudo, or
    /// <c>sudo -S</c> fed a password obtained via <see cref="IBootstrapElevationHandler"/> (prompting
    /// at most once per session unless the operator declines to have it remembered, or a prior
    /// password was rejected).
    /// </summary>
    public async Task<RemoteCommandResult> RunPrivilegedAsync(string command, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        if (PrivilegeMode == BootstrapPrivilegeMode.NotRequired)
        {
            return await ExecuteWithRecoveryAsync(() => executor.ExecuteCommandAsync(command, cancellationToken), command, elevated: false, cancellationToken);
        }

        var isRetryAfterRejection = false;
        while (true)
        {
            string? password = null;
            if (PrivilegeMode == BootstrapPrivilegeMode.PasswordRequired)
            {
                password = cachedSudoPassword ?? await RequestAndCacheSudoPasswordAsync(phase, isRetryAfterRejection, observer, cancellationToken);
            }

            observer?.OnPhaseProgress(phase, "Executing privileged operations...", null);
            var result = await ExecuteWithRecoveryAsync(
                () => executor.ExecuteElevatedCommandAsync(command, requiresSudo: true, password, cancellationToken),
                command,
                elevated: true,
                cancellationToken);

            if (PrivilegeMode == BootstrapPrivilegeMode.PasswordRequired && password is not null && IsSudoAuthFailure(result))
            {
                // The password we just tried was wrong — discard it (never retried automatically)
                // and loop back to prompt again. The operator's own Retry/Change Password/Cancel
                // choice happens inside RequestAndCacheSudoPasswordAsync's elevationHandler call.
                cachedSudoPassword = null;
                isRetryAfterRejection = true;
                continue;
            }

            return result;
        }
    }

    /// <summary>Guideline: "destroy on complete, fail, or cancel" — called from the orchestrator's own <c>finally</c> block.</summary>
    public void ClearSensitiveState() => cachedSudoPassword = null;

    /// <summary>
    /// Transient-failure recovery: a command that *throws* (SSH channel dropped, session expired,
    /// network blip — as opposed to returning a nonzero exit code, which is the command's own
    /// answer and is never retried here) is retried a bounded number of times after re-probing the
    /// connection, so a mid-bootstrap network interruption resumes instead of failing the run.
    /// Cancellation always propagates immediately.
    /// </summary>
    private async Task<RemoteCommandResult> ExecuteWithRecoveryAsync(Func<Task<RemoteCommandResult>> action, string command, bool elevated, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var startedUtc = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await action();
                stopwatch.Stop();
                Record(startedUtc, command, elevated, result.ExitCode, stopwatch.ElapsedMilliseconds, result.StandardError);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (attempt <= transientRetryAttempts)
            {
                stopwatch.Stop();
                Record(startedUtc, command, elevated, exitCode: -1, stopwatch.ElapsedMilliseconds, exception.Message);
                logger.LogWarning(exception, "Remote command threw (attempt {Attempt} of {Max}) — re-probing the connection and retrying: {Command}", attempt, transientRetryAttempts + 1, command);

                if (transientRetryDelayMs > 0)
                {
                    await Task.Delay(transientRetryDelayMs, cancellationToken);
                }

                try
                {
                    await executor.TestConnectionAsync(cancellationToken);
                }
                catch (Exception reconnectException)
                {
                    logger.LogWarning(reconnectException, "Reconnect probe itself threw; retrying the command regardless.");
                }
            }
        }
    }

    private void Record(DateTimeOffset startedUtc, string command, bool elevated, int exitCode, long durationMs, string standardError)
    {
        var firstErrorLine = standardError.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
        transcript.Add(new ExecutedCommandRecord(startedUtc, command, elevated, exitCode, durationMs, firstErrorLine));
    }

    private async Task<string> RequestAndCacheSudoPasswordAsync(BootstrapPhaseName phase, bool isRetryAfterRejection, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        if (elevationHandler is null)
        {
            throw new InvalidOperationException("This target requires a sudo password to continue, but no elevation handler was supplied to request one.");
        }

        observer?.OnPhaseProgress(
            phase,
            isRetryAfterRejection ? "Sudo password was rejected — waiting for a corrected password..." : "Waiting for administrator approval...",
            null);
        var prompt = await elevationHandler.RequestSudoPasswordAsync(isRetryAfterRejection, cancellationToken);
        if (prompt.Cancelled)
        {
            throw new OperationCanceledException("Sudo password was not supplied — bootstrap cancelled.");
        }

        if (prompt.Remember)
        {
            cachedSudoPassword = prompt.Password;
        }

        return prompt.Password!;
    }

    /// <summary>Recognizes sudo's own non-interactive rejection messages (sudo never retries itself when reading a password from a pipe rather than a TTY — it fails once and exits) so a wrong password becomes a clean re-prompt rather than a generic command failure.</summary>
    private static bool IsSudoAuthFailure(RemoteCommandResult result)
        => !result.Succeeded && (
            result.StandardError.Contains("incorrect password", StringComparison.OrdinalIgnoreCase)
            || result.StandardError.Contains("Sorry, try again", StringComparison.OrdinalIgnoreCase)
            || result.StandardError.Contains("no password was provided", StringComparison.OrdinalIgnoreCase)
            || result.StandardError.Contains("a password is required", StringComparison.OrdinalIgnoreCase));
}
