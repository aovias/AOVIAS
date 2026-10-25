using System.IO;
using ComplianceServiceManager.Models.SourceContext;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.WorkspaceRuntime;

namespace ComplianceServiceManager.Services.SourceContexts;

/// <summary>Validates source paths and branches against canonical Studio source contexts.</summary>
public static class SourceContextValidator
{
    public static bool IsUnderRoot(string path, string root)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        var full = Path.GetFullPath(path);
        var normalized = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return full.Equals(normalized, StringComparison.OrdinalIgnoreCase)
               || full.StartsWith(normalized + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || full.StartsWith(normalized + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAgentWorktreeRoot(string path, SourceContext shared, SourceContext cursor, SourceContext claude)
    {
        if (IsUnderRoot(path, cursor.RootPath) && !IsUnderRoot(path, shared.RootPath))
        {
            return true;
        }

        if (IsUnderRoot(path, claude.RootPath) && !IsUnderRoot(path, shared.RootPath))
        {
            return true;
        }

        return false;
    }

    public static bool AcceptsSourcePath(
        SourceContext context,
        string path,
        SourceContext shared,
        SourceContext cursor,
        SourceContext claude)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (context.Kind == SourceContextKind.Shared)
        {
            if (IsAgentWorktreeRoot(path, shared, cursor, claude))
            {
                return false;
            }

            return IsUnderRoot(path, context.RootPath);
        }

        if (IsUnderRoot(path, shared.RootPath) && !IsUnderRoot(path, context.RootPath))
        {
            return false;
        }

        if (IsUnderRoot(path, cursor.RootPath) && !IsUnderRoot(path, context.RootPath))
        {
            return false;
        }

        if (IsUnderRoot(path, claude.RootPath) && !IsUnderRoot(path, context.RootPath))
        {
            return false;
        }

        return IsUnderRoot(path, context.RootPath);
    }

    public static bool BranchMatchesExpected(SourceContext context, string? branch)
    {
        if (context.Kind == SourceContextKind.Shared)
        {
            return string.Equals(branch, "main", StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(context.WorkspaceId))
        {
            return false;
        }

        var prefix = WorkspaceSourceMapper.ExpectedBranchPrefix(context.WorkspaceId);
        return !string.IsNullOrWhiteSpace(branch)
               && branch.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    public static SourceContextGitValidity EvaluateGitValidity(
        SourceContextKind kind,
        string? workspaceId,
        bool isAccessible,
        bool isDetached,
        string? branch)
    {
        if (!isAccessible)
        {
            return SourceContextGitValidity.Unavailable;
        }

        if (isDetached)
        {
            return SourceContextGitValidity.Detached;
        }

        if (kind == SourceContextKind.Shared)
        {
            return string.Equals(branch, "main", StringComparison.OrdinalIgnoreCase)
                ? SourceContextGitValidity.Accessible
                : SourceContextGitValidity.InvalidBranch;
        }

        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return SourceContextGitValidity.InvalidBranch;
        }

        var prefix = WorkspaceSourceMapper.ExpectedBranchPrefix(workspaceId);
        return !string.IsNullOrWhiteSpace(branch) && branch.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? SourceContextGitValidity.Accessible
            : SourceContextGitValidity.InvalidBranch;
    }

    public static string ResolveOwnership(SourceContextKind kind, WorkspaceAgentOwner agent)
        => kind switch
        {
            SourceContextKind.Shared => "AOVIAS / Shared",
            SourceContextKind.Workspace when agent == WorkspaceAgentOwner.Cursor => "Cursor",
            SourceContextKind.Workspace when agent == WorkspaceAgentOwner.Claude => "Claude",
            SourceContextKind.Workspace => "Workspace",
            _ => "Unknown"
        };
}
