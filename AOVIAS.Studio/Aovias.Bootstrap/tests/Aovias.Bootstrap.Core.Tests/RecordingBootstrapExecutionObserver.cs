using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>Records every callback it receives, in order, so tests can assert both which events
/// fired and the sequence they fired in — the phase-lifecycle ordering guarantee
/// (<c>OnPhaseStarting</c> always immediately precedes the matching <c>OnPhaseFinished</c>, and
/// phases are reported strictly in ascending <see cref="BootstrapPhaseName"/> order) is exactly
/// what a UI observer depends on to attribute command/log events to the right stage.</summary>
public sealed class RecordingBootstrapExecutionObserver : IBootstrapExecutionObserver
{
    public List<BootstrapPhaseName> StartingPhases { get; } = [];

    public List<BootstrapPhaseResult> FinishedPhases { get; } = [];

    public List<string> StartedCommands { get; } = [];

    public List<(string Command, RemoteCommandResult Result)> FinishedCommands { get; } = [];

    public List<string> LogMessages { get; } = [];

    public List<(BootstrapPhaseName Phase, string Message, double? Fraction)> PhaseProgress { get; } = [];

    public void OnPhaseStarting(BootstrapPhaseName phase) => StartingPhases.Add(phase);

    public void OnPhaseFinished(BootstrapPhaseResult result) => FinishedPhases.Add(result);

    public void OnCommandStarting(string command) => StartedCommands.Add(command);

    public void OnCommandFinished(string command, RemoteCommandResult result) => FinishedCommands.Add((command, result));

    public void OnUploadProgress(string remotePath, ulong bytesTransferred, long totalBytes)
    {
        // Not exercised by FakeBootstrapTargetExecutor (in-memory, no real SFTP) — the byte-progress
        // path is covered by the observer's real wiring in SshBootstrapTargetExecutor, which needs a
        // real SSH/SFTP target to exercise and is therefore out of scope for these in-memory tests.
    }

    public void OnLog(string message) => LogMessages.Add(message);

    public void OnPhaseProgress(BootstrapPhaseName phase, string message, double? fraction) => PhaseProgress.Add((phase, message, fraction));
}
