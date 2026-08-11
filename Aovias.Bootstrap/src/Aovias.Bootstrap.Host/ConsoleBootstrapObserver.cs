using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Host;

/// <summary>
/// Reproduces the CLI's pre-existing console output (unchanged) plus real-time lines for commands,
/// uploads, and free-form log messages — visibility the CLI operator did not have before this
/// observer seam existed, at no cost to the final summary it already printed.
/// </summary>
public sealed class ConsoleBootstrapObserver : IBootstrapExecutionObserver
{
    private readonly Dictionary<string, int> lastReportedUploadPercent = new(StringComparer.Ordinal);

    public void OnPhaseStarting(BootstrapPhaseName phase)
        => Console.WriteLine($"> [{phase}] starting...");

    public void OnPhaseFinished(BootstrapPhaseResult result)
        => Console.WriteLine($"> [{result.Outcome}] {result.Phase}: {result.Message}");

    public void OnCommandStarting(string command)
        => Console.WriteLine($"    $ {command}");

    public void OnCommandFinished(string command, RemoteCommandResult result)
    {
        if (!result.Succeeded)
        {
            Console.WriteLine($"    ! exit {result.ExitCode}: {result.StandardError}");
        }
    }

    /// <summary>Throttled to whole-percent changes — SSH.NET's upload callback fires once per
    /// buffer chunk (potentially hundreds of times for a large package), and printing every one of
    /// those real events would drown the console without adding information.</summary>
    public void OnUploadProgress(string remotePath, ulong bytesTransferred, long totalBytes)
    {
        var percent = totalBytes > 0 ? (int)(bytesTransferred * 100 / (ulong)totalBytes) : 0;
        if (lastReportedUploadPercent.TryGetValue(remotePath, out var last) && last == percent)
        {
            return;
        }

        lastReportedUploadPercent[remotePath] = percent;
        Console.WriteLine($"    ↑ {remotePath}: {bytesTransferred}/{totalBytes} bytes ({percent}%)");
    }

    public void OnLog(string message)
        => Console.WriteLine($"    {message}");

    public void OnPhaseProgress(BootstrapPhaseName phase, string message, double? fraction)
        => Console.WriteLine(fraction is null ? $"    ~ {message}" : $"    ~ {message} ({fraction:P0})");
}
