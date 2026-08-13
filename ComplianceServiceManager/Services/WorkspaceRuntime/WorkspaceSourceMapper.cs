using System.IO;

namespace ComplianceServiceManager.Services.WorkspaceRuntime;

/// <summary>
/// Maps a discovered service path onto a target workspace root using the same relative
/// path under any known AOVIAS worktree. Never checks out branches.
/// </summary>
public static class WorkspaceSourceMapper
{
    public static string? ResolveUnderWorkspace(
        string discoveredPath,
        string targetWorkspaceRoot,
        IEnumerable<string> knownRoots)
    {
        if (string.IsNullOrWhiteSpace(discoveredPath) || string.IsNullOrWhiteSpace(targetWorkspaceRoot))
        {
            return null;
        }

        var full = Path.GetFullPath(discoveredPath);
        var target = TrimSlash(Path.GetFullPath(targetWorkspaceRoot));

        foreach (var root in knownRoots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            var normalized = TrimSlash(Path.GetFullPath(root));
            if (full.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return target;
            }

            if (full.StartsWith(normalized + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(normalized + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                var relative = full[normalized.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.Combine(target, relative);
            }
        }

        if (IsUnder(full, target))
        {
            return full;
        }

        return null;
    }

    public static bool IsUnder(string path, string root)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        var full = Path.GetFullPath(path);
        var normalized = TrimSlash(Path.GetFullPath(root));
        return full.Equals(normalized, StringComparison.OrdinalIgnoreCase)
               || full.StartsWith(normalized + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || full.StartsWith(normalized + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static string ExpectedBranchPrefix(string workspaceId)
        => workspaceId.Equals("cursor", StringComparison.OrdinalIgnoreCase)
            ? "agent/cursor/"
            : workspaceId.Equals("claude", StringComparison.OrdinalIgnoreCase)
                ? "agent/claude/"
                : string.Empty;

    private static string TrimSlash(string path)
        => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
