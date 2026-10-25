using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Execution;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Resolves optional git commit metadata for source revision calculation.
/// </summary>
public static class PublishingGitRevisionResolver
{
    public static string? TryResolveCommit(string? projectRoot, IProcessRunner processRunner)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
        {
            return null;
        }

        var root = FindGitRoot(projectRoot);
        if (root is null)
        {
            return null;
        }

        try
        {
            var result = processRunner.RunAsync(
                "git",
                "-C " + Quote(root) + " rev-parse HEAD",
                root,
                10,
                CancellationToken.None).GetAwaiter().GetResult();

            return result.IsSuccess
                ? result.StandardOutput.Trim().ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindGitRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
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

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
}
