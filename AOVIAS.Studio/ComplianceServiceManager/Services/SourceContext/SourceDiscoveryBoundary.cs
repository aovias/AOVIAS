using System.IO;
using ComplianceServiceManager.Models.SourceContext;
using ComplianceServiceManager.Services.LocalDev;

namespace ComplianceServiceManager.Services.SourceContexts;

/// <summary>
/// Maps a <see cref="SourceContext"/> to filesystem discovery roots.
/// Shared contexts never consume workspace solution-folder preferences.
/// </summary>
public interface ISourceDiscoveryBoundary
{
    IReadOnlyList<string> GetDiscoveryRoots(SourceContext context);
}

/// <inheritdoc />
public sealed class SourceDiscoveryBoundary : ISourceDiscoveryBoundary
{
    private readonly SolutionFolderRegistry folderRegistry;

    public SourceDiscoveryBoundary(SolutionFolderRegistry folderRegistry)
    {
        this.folderRegistry = folderRegistry;
    }

    public IReadOnlyList<string> GetDiscoveryRoots(SourceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Kind == SourceContextKind.Shared)
        {
            return GetDefaultApplicationRoots(context.RootPath);
        }

        var workspaceId = context.WorkspaceId ?? context.ContextId;
        var configured = folderRegistry.GetDiscoveryRootsForWorkspace(workspaceId, context.RootPath);
        return configured.Count > 0
            ? configured
            : GetDefaultApplicationRoots(context.RootPath);
    }

    internal static IReadOnlyList<string> GetDefaultApplicationRoots(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return Array.Empty<string>();
        }

        var roots = new List<string>();
        TryAddRoot(roots, Path.Combine(rootPath, "AOVIAS.Command", "ComplaincePlatform"));
        TryAddRoot(roots, Path.Combine(rootPath, "AOVIAS.Command", "WebApp"));
        TryAddRoot(roots, Path.Combine(rootPath, "ComplaincePlatform"));
        TryAddRoot(roots, Path.Combine(rootPath, "WebApp"));
        return roots;

        static void TryAddRoot(List<string> roots, string candidate)
        {
            if (Directory.Exists(candidate))
            {
                roots.Add(Path.GetFullPath(candidate));
            }
        }
    }
}
