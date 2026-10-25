using System.Diagnostics;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Execution;

namespace Aovias.Publishing.Core.Pipeline;

/// <summary>
/// Captures environment metadata for reproducible publishing sessions.
/// </summary>
public static class PublishingEnvironmentProbe
{
    public static async Task PopulateAsync(PublishingSession session, CancellationToken cancellationToken)
    {
        session.GetType(); // session is init-only; values set via copy in orchestrator if needed
        _ = session;
        _ = cancellationToken;
        await Task.CompletedTask;
    }

    public static async Task<PublishingSession> EnrichAsync(PublishingSession session, IDockerCliExecutor? dockerCliExecutor, CancellationToken cancellationToken)
    {
        string? dockerVersion = null;
        if (dockerCliExecutor is not null)
        {
            var inspect = await dockerCliExecutor.InspectAsync(cancellationToken);
            dockerVersion = inspect.IsSuccess ? inspect.StandardOutput.Trim() : inspect.StandardError.Trim();
        }

        var dotnetVersion = await TryRunAsync("dotnet", "--version", cancellationToken);
        var gitCommit = await TryRunAsync("git", "rev-parse HEAD", cancellationToken);

        return session with
        {
            DockerVersion = string.IsNullOrWhiteSpace(dockerVersion) ? null : dockerVersion,
            DotNetSdkVersion = string.IsNullOrWhiteSpace(dotnetVersion) ? null : dotnetVersion.Trim(),
            GitCommit = string.IsNullOrWhiteSpace(gitCommit) ? null : gitCommit.Trim()
        };
    }

    private static async Task<string> TryRunAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return string.Empty;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }
}
