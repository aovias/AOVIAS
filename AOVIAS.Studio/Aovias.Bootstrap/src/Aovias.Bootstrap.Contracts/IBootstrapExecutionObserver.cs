namespace Aovias.Bootstrap.Contracts;

/// <summary>
/// Live execution signals for one <see cref="IBootstrapOrchestrator.BootstrapAsync"/> run, so a
/// caller (the CLI's console output, or AOVIAS Studio's Operation Engine) can present real-time
/// progress instead of only the final <see cref="BootstrapReport"/>. Optional everywhere it is
/// threaded through (<see cref="IBootstrapOrchestrator.BootstrapAsync"/>,
/// <c>SshBootstrapTargetExecutor</c>, <c>BootstrapRunner</c>) — every existing caller that does not
/// supply one keeps working unchanged.
///
/// Deliberately a callback interface rather than a single <c>IProgress&lt;T&gt;</c>: there are
/// several distinct signal kinds (phase lifecycle, remote command lifecycle, SFTP upload byte
/// progress, free-form log lines) that a UI wants to route differently, and folding them into one
/// generic progress type would force an artificial discriminated-union payload for no benefit.
///
/// Every event here reports something that actually happened — no event is raised speculatively or
/// on a timer. Command/upload/log events do not carry a <see cref="BootstrapPhaseName"/> parameter:
/// phases run strictly sequentially (see <c>BootstrapOrchestrator.BootstrapAsync</c>'s single
/// foreach loop), so a subscriber can always attribute a command/upload/log event to whichever
/// phase most recently raised <see cref="OnPhaseStarting"/> without needing it repeated on every
/// call — the same principle the CLI's own final report already relies on implicitly.
/// </summary>
public interface IBootstrapExecutionObserver
{
    /// <summary>A phase is about to run (or, if already recorded complete, is about to be reported as such — see <see cref="OnPhaseFinished"/>).</summary>
    void OnPhaseStarting(BootstrapPhaseName phase);

    /// <summary>A phase finished, whether it ran, was already satisfied, or failed.</summary>
    void OnPhaseFinished(BootstrapPhaseResult result);

    /// <summary>A remote shell command is about to execute.</summary>
    void OnCommandStarting(string command);

    /// <summary>A remote shell command finished.</summary>
    void OnCommandFinished(string command, RemoteCommandResult result);

    /// <summary>Real SFTP upload progress for one file — sourced directly from SSH.NET's upload byte-count callback, never simulated.</summary>
    void OnUploadProgress(string remotePath, ulong bytesTransferred, long totalBytes);

    /// <summary>A free-form informational line (e.g. "Testing connection to target host...") that does not correspond to a discrete command.</summary>
    void OnLog(string message);

    /// <summary>
    /// Fine-grained progress within a single phase that does not correspond to a discrete remote
    /// command or file upload — e.g. "Polling Runtime Agent heartbeat — attempt 3 of 10" during
    /// <see cref="BootstrapPhaseName.BootstrapVerification"/>, or which specific preflight check is
    /// currently running. <paramref name="fraction"/> is a real 0.0–1.0 signal when one exists
    /// (e.g. attempt count / max attempts), otherwise <see langword="null"/> — never a guessed value.
    /// </summary>
    void OnPhaseProgress(BootstrapPhaseName phase, string message, double? fraction);
}
