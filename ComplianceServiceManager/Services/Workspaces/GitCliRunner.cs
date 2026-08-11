using System.Diagnostics;
using System.IO;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>
/// Executes only the <c>git</c> executable with caller-supplied arguments against an allowlisted
/// working directory. Does not accept arbitrary executables or shell strings.
/// </summary>
public sealed class GitCliRunner
{
    private readonly Func<IReadOnlyCollection<string>> allowedRootsProvider;

    public GitCliRunner(Func<IReadOnlyCollection<string>> allowedRootsProvider)
    {
        this.allowedRootsProvider = allowedRootsProvider;
    }

    public async Task<GitCliResult> RunAsync(
        string workingDirectory,
        string arguments,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new InvalidOperationException("Git working directory is required.");
        }

        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException($"Git working directory does not exist: {workingDirectory}");
        }

        var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
        EnsureAllowed(normalizedWorkingDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = normalizedWorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to start the git executable.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new GitCliResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout ?? string.Empty,
            StandardError = stderr ?? string.Empty
        };
    }

    private void EnsureAllowed(string normalizedWorkingDirectory)
    {
        var roots = allowedRootsProvider()
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(root))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roots.Count == 0)
        {
            throw new InvalidOperationException("No allowed Git repository roots are configured.");
        }

        var allowed = roots.Any(root =>
            string.Equals(normalizedWorkingDirectory, root, StringComparison.OrdinalIgnoreCase)
            || IsUnderRoot(normalizedWorkingDirectory, root));

        if (!allowed)
        {
            throw new UnauthorizedAccessException(
                $"Git operations are not permitted outside configured development repository roots.");
        }
    }

    private static bool IsUnderRoot(string path, string root)
    {
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }
}
