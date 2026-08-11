namespace Aovias.Bootstrap.Contracts;

/// <summary>The result of running one remote shell command.</summary>
public sealed class RemoteCommandResult
{
    public int ExitCode { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Guideline #9 ("no direct OS/Docker/filesystem/network calls from core logic — new external
/// touchpoints get a provider interface first"): every remote side effect Bootstrap performs goes
/// through this single seam. <c>Aovias.Bootstrap.Host</c> supplies the real, SSH.NET-backed
/// implementation; <c>Aovias.Bootstrap.Core.Tests</c> supplies an in-memory fake so phase logic is
/// exercised without a real network or a real VPS. Deliberately a new, separate interface from
/// <c>IDockerCliExecutor</c>/<c>IDockerProvider</c> — those run commands against the local Docker
/// daemon this process is co-located with; this one runs commands over SSH against a remote host
/// that has no agent (and therefore no in-process abstraction) running on it yet.
/// </summary>
public interface IBootstrapTargetExecutor
{
    /// <summary>Opens (or reuses) the connection to the target and confirms a trivial command succeeds. Never throws for a routine connection failure — returns a failed result instead so <c>Preflight</c> can report it as a normal phase failure.</summary>
    Task<RemoteCommandResult> TestConnectionAsync(CancellationToken cancellationToken);

    Task<RemoteCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken);

    Task<bool> RemotePathExistsAsync(string remotePath, CancellationToken cancellationToken);

    /// <summary>Writes <paramref name="content"/> to <paramref name="remotePath"/>, creating parent directories as needed.</summary>
    Task UploadTextFileAsync(string remotePath, string content, CancellationToken cancellationToken);

    /// <summary>Uploads the local file at <paramref name="localPath"/> to <paramref name="remotePath"/>.</summary>
    Task UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken);

    /// <summary>Computes a SHA-256 checksum (lowercase hex) of the local file at <paramref name="localPath"/>, read on the machine running Bootstrap — never the remote target. Used to verify an uploaded package's integrity by comparing against a checksum computed remotely (<c>sha256sum</c>) after upload.</summary>
    Task<string> ComputeLocalFileChecksumAsync(string localPath, CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="command"/> with root privileges using whichever elevation strategy the
    /// caller has already determined applies — this method makes no privilege decisions of its own.
    /// <paramref name="requiresSudo"/> false means the SSH user is already root (runs the command
    /// as-is). <paramref name="requiresSudo"/> true with <paramref name="sudoPassword"/> null means
    /// passwordless sudo (<c>sudo -n</c>). <paramref name="requiresSudo"/> true with a non-null
    /// <paramref name="sudoPassword"/> means password-based sudo (<c>sudo -S</c>) — the real
    /// implementation must supply the password only via the remote command's own stdin over the
    /// already-open SSH channel, never interpolated into the command string (which would leak it to
    /// a <c>ps</c> listing, shell history, or audit logs) and never written to local disk.
    /// </summary>
    Task<RemoteCommandResult> ExecuteElevatedCommandAsync(string command, bool requiresSudo, string? sudoPassword, CancellationToken cancellationToken);
}
