using System.IO;
using System.Text.Json;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>
/// Persists Studio workspace configuration under %LocalAppData%\AoviasStudio\.
/// Git-derived state is never stored as authoritative data.
/// </summary>
public sealed class DevelopmentWorkspaceRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string storePath;
    private readonly object gate = new();
    private DevelopmentWorkspaceRegistryDocument document;

    public DevelopmentWorkspaceRegistry(string? storePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        this.storePath = storePath ?? Path.Combine(root, "development-workspaces.json");
        document = Load();
    }

    public string StorePath => storePath;

    public DevelopmentWorkspaceRegistryDocument GetDocument()
    {
        lock (gate)
        {
            return Clone(document);
        }
    }

    public IReadOnlyList<string> GetAllowedRoots()
    {
        lock (gate)
        {
            return document.Repositories
                .SelectMany(repository => repository.Workspaces
                    .Where(workspace => workspace.Enabled)
                    .Select(workspace => workspace.Path)
                    .Append(repository.RootPath))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<ConfiguredWorkspace> GetEnabledWorkspaces()
    {
        lock (gate)
        {
            return document.Repositories
                .SelectMany(repository => repository.Workspaces
                    .Where(workspace => workspace.Enabled)
                    .Select(workspace => CloneWorkspace(workspace)))
                .ToList();
        }
    }

    public IReadOnlyList<(ConfiguredRepository Repository, ConfiguredWorkspace Workspace)> GetEnabledWorkspacePairs()
    {
        lock (gate)
        {
            return document.Repositories
                .SelectMany(repository => repository.Workspaces
                    .Where(workspace => workspace.Enabled)
                    .Select(workspace => (CloneRepository(repository), CloneWorkspace(workspace))))
                .ToList();
        }
    }

    private DevelopmentWorkspaceRegistryDocument Load()
    {
        try
        {
            if (File.Exists(storePath))
            {
                var loaded = JsonSerializer.Deserialize<DevelopmentWorkspaceRegistryDocument>(
                    File.ReadAllText(storePath),
                    JsonOptions);
                if (loaded?.Repositories is { Count: > 0 })
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt preference file: fall back to defaults.
        }

        var defaults = BuildDefaults();
        try
        {
            File.WriteAllText(storePath, JsonSerializer.Serialize(defaults, JsonOptions));
        }
        catch
        {
            // Best-effort persistence.
        }

        return defaults;
    }

    internal static DevelopmentWorkspaceRegistryDocument BuildDefaults(string? developmentRepoRoot = null)
    {
        var root = developmentRepoRoot;
        if (string.IsNullOrWhiteSpace(root))
        {
            var platformRoot = RootPathResolver.ResolvePlatformRoot();
            root = Directory.GetParent(platformRoot)?.FullName
                   ?? Path.GetFullPath(Path.Combine(platformRoot, ".."));
        }

        root = Path.GetFullPath(root);
        var mainPath = Path.Combine(root, "AOVIAS");
        var cursorPath = Path.Combine(root, "AOVIAS.Cursor");
        var claudePath = Path.Combine(root, "AOVIAS.Claude");

        return new DevelopmentWorkspaceRegistryDocument
        {
            Repositories =
            [
                new ConfiguredRepository
                {
                    Id = "aovias",
                    Name = "AOVIAS",
                    RootPath = mainPath,
                    DefaultBranch = "main",
                    Workspaces =
                    [
                        new ConfiguredWorkspace
                        {
                            Id = "main",
                            DisplayName = "Shared",
                            Path = mainPath,
                            AgentOwner = WorkspaceAgentOwner.Integration,
                            Role = WorkspaceRole.Integration,
                            IsMain = true,
                            Enabled = true
                        },
                        new ConfiguredWorkspace
                        {
                            Id = "cursor",
                            DisplayName = "Cursor",
                            Path = cursorPath,
                            AgentOwner = WorkspaceAgentOwner.Cursor,
                            Role = WorkspaceRole.Agent,
                            IsMain = false,
                            Enabled = true
                        },
                        new ConfiguredWorkspace
                        {
                            Id = "claude",
                            DisplayName = "Claude",
                            Path = claudePath,
                            AgentOwner = WorkspaceAgentOwner.Claude,
                            Role = WorkspaceRole.Agent,
                            IsMain = false,
                            Enabled = true
                        }
                    ]
                }
            ]
        };
    }

    private static DevelopmentWorkspaceRegistryDocument Clone(DevelopmentWorkspaceRegistryDocument source)
        => new()
        {
            Repositories = source.Repositories.Select(CloneRepository).ToList()
        };

    private static ConfiguredRepository CloneRepository(ConfiguredRepository source)
        => new()
        {
            Id = source.Id,
            Name = source.Name,
            RootPath = source.RootPath,
            DefaultBranch = source.DefaultBranch,
            Workspaces = source.Workspaces.Select(CloneWorkspace).ToList()
        };

    private static ConfiguredWorkspace CloneWorkspace(ConfiguredWorkspace source)
        => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName,
            Path = source.Path,
            AgentOwner = source.AgentOwner,
            Role = source.Role,
            IsMain = source.IsMain,
            Enabled = source.Enabled
        };
}
