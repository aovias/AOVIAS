using System.Security.Cryptography;
using System.Text;
using Aovias.Bootstrap.Contracts;
using Renci.SshNet;

namespace Aovias.Bootstrap.Execution;

/// <summary>
/// The real, SSH.NET-backed <see cref="IBootstrapTargetExecutor"/> — the only place in Bootstrap
/// that actually opens a network connection or touches a remote filesystem. Everything in
/// <c>Aovias.Bootstrap.Core</c> depends on the interface, not this class, so phase logic is
/// exercised in tests without SSH.NET or a real host (Guideline #9).
///
/// Moved from <c>Aovias.Bootstrap.Host</c> into this shared execution project so both the CLI and
/// AOVIAS Studio construct and use the exact same class — there is no second SSH implementation.
/// </summary>
public sealed class SshBootstrapTargetExecutor : IBootstrapTargetExecutor, IDisposable
{
    private readonly BootstrapTarget target;
    private readonly IBootstrapExecutionObserver? observer;
    private SshClient? sshClient;

    public SshBootstrapTargetExecutor(BootstrapTarget target, IBootstrapExecutionObserver? observer = null)
    {
        this.target = target;
        this.observer = observer;
    }

    public async Task<RemoteCommandResult> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            observer?.OnLog($"Connecting to {target.Host}:{target.Port}...");
            var client = Connect();
            return await Task.FromResult(new RemoteCommandResult { ExitCode = client.IsConnected ? 0 : 1 });
        }
        catch (Exception exception)
        {
            return new RemoteCommandResult { ExitCode = 1, StandardError = exception.Message };
        }
    }

    public Task<RemoteCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        // SSH.NET's CreateCommand/Execute is fully synchronous — there is no way to interrupt a
        // command already in flight. This check is what makes cancellation actually take effect: it
        // stops the *next* command in a phase (a phase like RuntimeAgentInstallation runs several)
        // rather than doing nothing at all, matching "finishes the current remote command, then
        // stops" — never claiming an immediate abort this transport cannot deliver.
        cancellationToken.ThrowIfCancellationRequested();
        observer?.OnCommandStarting(command);
        var client = Connect();
        using var sshCommand = client.CreateCommand(command);
        var output = sshCommand.Execute();
        var result = new RemoteCommandResult
        {
            ExitCode = sshCommand.ExitStatus ?? -1,
            StandardOutput = output,
            StandardError = sshCommand.Error
        };
        observer?.OnCommandFinished(command, result);
        return Task.FromResult(result);
    }

    public async Task<bool> RemotePathExistsAsync(string remotePath, CancellationToken cancellationToken)
    {
        var result = await ExecuteCommandAsync($"test -e {remotePath}", cancellationToken);
        return result.Succeeded;
    }

    public async Task UploadTextFileAsync(string remotePath, string content, CancellationToken cancellationToken)
    {
        var directory = remotePath[..remotePath.LastIndexOf('/')];
        await ExecuteCommandAsync($"mkdir -p {directory}", cancellationToken);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        UploadViaSftp(stream, remotePath);
    }

    public async Task UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken)
    {
        var directory = remotePath[..remotePath.LastIndexOf('/')];
        await ExecuteCommandAsync($"mkdir -p {directory}", cancellationToken);

        using var stream = File.OpenRead(localPath);
        UploadViaSftp(stream, remotePath);
    }

    public async Task<string> ComputeLocalFileChecksumAsync(string localPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(localPath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<RemoteCommandResult> ExecuteElevatedCommandAsync(string command, bool requiresSudo, string? sudoPassword, CancellationToken cancellationToken)
    {
        if (!requiresSudo)
        {
            return await ExecuteCommandAsync(command, cancellationToken);
        }

        if (string.IsNullOrEmpty(sudoPassword))
        {
            // Passwordless sudo. `-n` fails fast rather than hanging on a phantom password prompt
            // if host state changed between Preflight's detection and now.
            return await ExecuteCommandAsync($"sudo -n {command}", cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Password-based elevation: the password is supplied only via this command's own input
        // stream over the already-open SSH channel (SshCommand.CreateInputStream — SSH.NET has no
        // settable InputStream property) — never interpolated into the command string (which would
        // leak it to a remote `ps` listing, shell history, or audit logs) and never written to local
        // disk. `-p ''` suppresses sudo's own password-prompt text, which would otherwise appear in
        // StandardError for no reason (we never display or need it).
        var displayCommand = $"sudo -S {command}";
        observer?.OnCommandStarting(displayCommand);
        var client = Connect();
        using var sshCommand = client.CreateCommand($"sudo -S -p '' {command}");
        var executeTask = sshCommand.ExecuteAsync(cancellationToken);
        var passwordBytes = Encoding.UTF8.GetBytes(sudoPassword + "\n");
        await using (var inputStream = sshCommand.CreateInputStream())
        {
            await inputStream.WriteAsync(passwordBytes, cancellationToken);
        }

        await executeTask;
        var result = new RemoteCommandResult
        {
            ExitCode = sshCommand.ExitStatus ?? -1,
            StandardOutput = sshCommand.Result,
            StandardError = sshCommand.Error
        };
        observer?.OnCommandFinished(displayCommand, result);
        return result;
    }

    /// <summary>Uses <see cref="SftpClient"/> for uploads — the same SSH.NET type this assembly's
    /// existing "deploy" verb (<c>Program.cs</c>'s <c>UploadFile</c> local function) already uses,
    /// rather than introducing <c>ScpClient</c> as a second, unverified upload path. Reports real
    /// byte-level progress via SSH.NET's own upload callback — never simulated.</summary>
    private void UploadViaSftp(Stream stream, string remotePath)
    {
        using var sftp = new SftpClient(BuildConnectionInfo());
        sftp.Connect();
        var totalBytes = stream.Length;
        sftp.UploadFile(stream, remotePath, true, bytesTransferred => observer?.OnUploadProgress(remotePath, bytesTransferred, totalBytes));
        sftp.Disconnect();
    }

    private SshClient Connect()
    {
        if (sshClient is { IsConnected: true })
        {
            return sshClient;
        }

        sshClient = new SshClient(BuildConnectionInfo());
        sshClient.Connect();
        return sshClient;
    }

    private ConnectionInfo BuildConnectionInfo()
    {
        AuthenticationMethod authentication = !string.IsNullOrWhiteSpace(target.PrivateKeyPath)
            ? new PrivateKeyAuthenticationMethod(target.Username, new PrivateKeyFile(target.PrivateKeyPath))
            : new PasswordAuthenticationMethod(target.Username, target.Password ?? string.Empty);

        return new ConnectionInfo(target.Host, target.Port, target.Username, authentication);
    }

    public void Dispose()
    {
        if (sshClient is not null)
        {
            if (sshClient.IsConnected)
            {
                sshClient.Disconnect();
            }

            sshClient.Dispose();
        }
    }
}
