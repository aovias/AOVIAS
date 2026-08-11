using System.Diagnostics;
using System.Text;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using System.IO;

namespace ComplianceServiceManager.Services;

public sealed class GitSafetyService : IGitSafetyService
{
    private readonly ILoggingService logger;

    public GitSafetyService(ILoggingService logger)
    {
        this.logger = logger;
    }

    public async Task<string> CreateCheckpointAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var root = ResolveRepositoryRoot(repositoryPath);
        if (root is null)
        {
            return "git-not-found";
        }

        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        var branchName = $"fix/session/{stamp}";
        await RunGitAsync(root, $"checkout -b {branchName}", cancellationToken);
        logger.Info("GitSafety", $"Created repair checkpoint branch {branchName}");
        return branchName;
    }

    public async Task RollbackAsync(string repositoryPath, string checkpointReference, CancellationToken cancellationToken)
    {
        var root = ResolveRepositoryRoot(repositoryPath);
        if (root is null || checkpointReference == "git-not-found")
        {
            return;
        }

        await RunGitAsync(root, "reset --hard", cancellationToken);
        await RunGitAsync(root, "clean -fd", cancellationToken);
        await RunGitAsync(root, $"checkout {checkpointReference}", cancellationToken);
        logger.Warning("GitSafety", $"Rolled back to checkpoint {checkpointReference}");
    }

    public async Task<IReadOnlyList<string>> GetModifiedFilesAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var root = ResolveRepositoryRoot(repositoryPath);
        if (root is null)
        {
            return Array.Empty<string>();
        }

        var output = await CaptureGitOutputAsync(root, "status --short", cancellationToken);
        return output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Length > 3 ? line[3..] : line)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ResolveRepositoryRoot(string path)
    {
        var current = new DirectoryInfo(path);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private async Task RunGitAsync(string workingDirectory, string args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            return;
        }

        await process.WaitForExitAsync(cancellationToken);
    }

    private static async Task<string> CaptureGitOutputAsync(string workingDirectory, string args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            return string.Empty;
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }
}
