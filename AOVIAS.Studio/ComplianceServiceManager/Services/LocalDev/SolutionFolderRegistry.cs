using System.IO;
using System.Text.Json;
using ComplianceServiceManager.Models.SourceContext;

namespace ComplianceServiceManager.Services.LocalDev;

/// <summary>
/// Persists workspace-scoped discovery roots for Local Development (ComplaincePlatform, WebApp, etc.).
/// These preferences configure which folders are scanned when discovering services for a workspace
/// source context. They do not define Shared Runtime canonical source — Shared discovery always
/// uses the integration worktree via <see cref="SourceContext"/>.
/// </summary>
public sealed class SolutionFolderRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string storePath;
    private readonly object gate = new();
    private List<string> folders;

    public event Action? FoldersChanged;

    public SolutionFolderRegistry(string? storePathOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(storePathOverride))
        {
            var directory = Path.GetDirectoryName(storePathOverride);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            storePath = storePathOverride;
            folders = Load();
            return;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        storePath = Path.Combine(root, "solution-folders.json");
        folders = Load();
    }

    /// <summary>All configured workspace discovery roots (UI preference store).</summary>
    public IReadOnlyList<string> Folders
    {
        get
        {
            lock (gate)
            {
                return folders.ToList();
            }
        }
    }

    public void Add(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        var normalized = Path.GetFullPath(folder);
        lock (gate)
        {
            if (folders.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            folders.Add(normalized);
            Save();
        }

        FoldersChanged?.Invoke();
    }

    public void Remove(string folder)
    {
        lock (gate)
        {
            var removed = folders.RemoveAll(existing => string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return;
            }

            Save();
        }

        FoldersChanged?.Invoke();
    }

    /// <summary>
    /// Discovery roots for a workspace source context: configured folders under the workspace root,
    /// excluding paths outside that workspace boundary.
    /// </summary>
    public IReadOnlyList<string> GetDiscoveryRootsForWorkspace(string workspaceId, string workspaceRoot)
    {
        _ = workspaceId;
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            return Array.Empty<string>();
        }

        var normalizedRoot = Path.GetFullPath(workspaceRoot);
        lock (gate)
        {
            return folders
                .Where(folder => IsUnderRoot(folder, normalizedRoot))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private List<string> Load()
    {
        try
        {
            if (File.Exists(storePath))
            {
                var stored = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(storePath));
                if (stored is { Count: > 0 })
                {
                    return stored.Where(Directory.Exists).ToList();
                }
            }
        }
        catch
        {
            // Corrupt preference file: fall back to defaults rather than failing startup.
        }

        return BuildDefaults();
    }

    private static List<string> BuildDefaults()
    {
        // Workspace discovery roots are intentionally empty by default.
        // Shared and workspace contexts derive canonical/default roots from SourceContext.RootPath.
        return [];
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(storePath, JsonSerializer.Serialize(folders, JsonOptions));
        }
        catch
        {
            // Preference persistence is best-effort; the in-memory list keeps working.
        }
    }

    private static bool IsUnderRoot(string path, string root)
    {
        var full = Path.GetFullPath(path);
        var normalized = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return full.Equals(normalized, StringComparison.OrdinalIgnoreCase)
               || full.StartsWith(normalized + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || full.StartsWith(normalized + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
