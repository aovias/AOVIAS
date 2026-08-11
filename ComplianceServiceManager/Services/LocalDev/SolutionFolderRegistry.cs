using System.IO;
using System.Text.Json;

namespace ComplianceServiceManager.Services.LocalDev;

/// <summary>
/// Persists the set of local solution folders the Local Dev Services workspace manages.
/// Purely a UI-side preference store: it never talks to the platform. Defaults to the
/// repository's discovered application folders so first launch behaves exactly like the
/// pre-Studio Services workspace did.
/// </summary>
public sealed class SolutionFolderRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string storePath;
    private readonly object gate = new();
    private List<string> folders;

    public event Action? FoldersChanged;

    public SolutionFolderRegistry()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        storePath = Path.Combine(root, "solution-folders.json");
        folders = Load();
    }

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
        var defaults = new List<string>();
        var complianceRoot = RootPathResolver.ResolveCompliancePlatformRoot();
        if (Directory.Exists(complianceRoot))
        {
            defaults.Add(complianceRoot);
        }

        var webAppRoot = RootPathResolver.ResolveWebAppRoot();
        if (Directory.Exists(webAppRoot))
        {
            defaults.Add(webAppRoot);
        }

        return defaults;
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
}
