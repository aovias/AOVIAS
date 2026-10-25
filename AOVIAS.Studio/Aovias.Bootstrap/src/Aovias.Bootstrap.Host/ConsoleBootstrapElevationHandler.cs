using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Host;

/// <summary>
/// The CLI's <see cref="IBootstrapElevationHandler"/>. Root-cause fix for a real-VPS failure: the
/// <c>bootstrap</c> verb never supplied an elevation handler at all, so any target whose user needs
/// password sudo (<see cref="BootstrapPrivilegeMode.PasswordRequired"/> — e.g. a hardened host
/// where root SSH is disabled and the service account is in the sudo group) failed its very first
/// privileged command with "no elevation handler was supplied". Resolution order:
///
/// 1. An explicitly supplied sudo password (<c>--sudo-password</c>) — the non-interactive path an
///    autonomous/CI invocation uses.
/// 2. The SSH password (<c>--password</c>), when one was supplied — on typical hosts the account's
///    SSH and sudo passwords are the same credential; key-authenticated targets skip this.
/// 3. An interactive, echo-free console prompt — only when stdin is a real terminal; a redirected/
///    headless invocation gets a clean "cancelled" answer instead of hanging forever on a prompt
///    nobody can see.
///
/// A password rejected by sudo on the target invalidates the scripted sources (retrying the same
/// wrong value forever would loop) and falls through to the interactive prompt or a clean cancel.
/// Per the interface contract, nothing here writes the password to disk, a log, or any diagnostic
/// surface.
/// </summary>
internal sealed class ConsoleBootstrapElevationHandler(string? sudoPassword, string? sshPassword) : IBootstrapElevationHandler
{
    private bool scriptedSourcesInvalidated;

    public Task<SudoPasswordPromptResult> RequestSudoPasswordAsync(bool isRetryAfterRejection, CancellationToken cancellationToken)
    {
        if (isRetryAfterRejection)
        {
            scriptedSourcesInvalidated = true;
        }

        if (!scriptedSourcesInvalidated)
        {
            var scripted = sudoPassword ?? sshPassword;
            if (!string.IsNullOrEmpty(scripted))
            {
                return Task.FromResult(new SudoPasswordPromptResult { Password = scripted, Remember = true });
            }
        }

        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine(isRetryAfterRejection
                ? "Sudo password was rejected by the target and no interactive terminal is available to ask for a corrected one — pass --sudo-password."
                : "This target requires a sudo password and no interactive terminal is available — pass --sudo-password (or --password when the SSH and sudo passwords match).");
            return Task.FromResult(new SudoPasswordPromptResult { Password = null });
        }

        Console.Write(isRetryAfterRejection
            ? "Sudo password was rejected — enter the correct sudo password (empty to cancel): "
            : "This target requires a sudo password — enter it (empty to cancel): ");
        var entered = ReadPasswordWithoutEcho();
        return Task.FromResult(string.IsNullOrEmpty(entered)
            ? new SudoPasswordPromptResult { Password = null }
            : new SudoPasswordPromptResult { Password = entered, Remember = true });
    }

    private static string ReadPasswordWithoutEcho()
    {
        var buffer = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
            }
        }
    }
}
