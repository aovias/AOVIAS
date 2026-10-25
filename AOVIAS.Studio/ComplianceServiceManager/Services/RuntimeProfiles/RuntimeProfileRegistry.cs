using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Models.RuntimeProfiles;

namespace ComplianceServiceManager.Services.RuntimeProfiles;

/// <summary>
/// Persists runtime profile definitions under %LocalAppData%\AoviasStudio\.
/// Process/health state is never stored as authoritative configuration.
/// </summary>
public sealed class RuntimeProfileRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string storePath;
    private readonly object gate = new();
    private RuntimeProfileRegistryDocument document;

    public RuntimeProfileRegistry(string? storePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        this.storePath = storePath ?? Path.Combine(root, "runtime-profiles.json");
        document = Load();
    }

    public string StorePath => storePath;

    public IReadOnlyList<RuntimeProfileDefinition> GetProfiles()
    {
        lock (gate)
        {
            return document.Profiles.Select(Clone).ToList();
        }
    }

    public RuntimeProfileDefinition? GetById(string id)
    {
        lock (gate)
        {
            var match = document.Profiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : Clone(match);
        }
    }

    public void SaveAll(IEnumerable<RuntimeProfileDefinition> profiles)
    {
        lock (gate)
        {
            document = new RuntimeProfileRegistryDocument
            {
                Profiles = profiles.Select(Clone).ToList()
            };
            Persist();
        }
    }

    public void Upsert(RuntimeProfileDefinition profile)
    {
        lock (gate)
        {
            var index = document.Profiles.FindIndex(existing =>
                string.Equals(existing.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
            var clone = Clone(profile);
            if (index >= 0)
            {
                document.Profiles[index] = clone;
            }
            else
            {
                document.Profiles.Add(clone);
            }

            Persist();
        }
    }

    public bool Remove(string id)
    {
        lock (gate)
        {
            var removed = document.Profiles.RemoveAll(profile =>
                string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return false;
            }

            Persist();
            return true;
        }
    }

    private RuntimeProfileRegistryDocument Load()
    {
        try
        {
            if (File.Exists(storePath))
            {
                var loaded = JsonSerializer.Deserialize<RuntimeProfileRegistryDocument>(
                    File.ReadAllText(storePath),
                    JsonOptions);
                if (loaded?.Profiles is { Count: > 0 })
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt preference: fall back to defaults.
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

    private void Persist()
    {
        try
        {
            File.WriteAllText(storePath, JsonSerializer.Serialize(document, JsonOptions));
        }
        catch
        {
            // Best-effort.
        }
    }

    internal static RuntimeProfileRegistryDocument BuildDefaults()
        => new()
        {
            Profiles =
            [
                new RuntimeProfileDefinition
                {
                    Id = "aovias-web-development",
                    Name = "AOVIAS Web Development",
                    Description = "Minimal WebApp development stack: Identity, Organization, Compliance, Gateway, and WebApp. SQL Server must already be available locally.",
                    Enabled = true,
                    MaxParallelStarts = 2,
                    Services =
                    [
                        new RuntimeProfileServiceEntry
                        {
                            ServiceName = "IdentityService",
                            Enabled = true,
                            DependsOn = []
                        },
                        new RuntimeProfileServiceEntry
                        {
                            ServiceName = "OrganizationService",
                            Enabled = true,
                            DependsOn = []
                        },
                        new RuntimeProfileServiceEntry
                        {
                            ServiceName = "ComplianceService",
                            Enabled = true,
                            DependsOn = ["OrganizationService"]
                        },
                        new RuntimeProfileServiceEntry
                        {
                            ServiceName = "Gateway",
                            Enabled = true,
                            DependsOn = ["IdentityService", "ComplianceService", "OrganizationService"]
                        },
                        new RuntimeProfileServiceEntry
                        {
                            ServiceName = "WebApp",
                            Enabled = true,
                            DependsOn = ["Gateway"]
                        }
                    ]
                }
            ]
        };

    private static RuntimeProfileDefinition Clone(RuntimeProfileDefinition source)
        => new()
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Enabled = source.Enabled,
            MaxParallelStarts = source.MaxParallelStarts <= 0 ? 2 : source.MaxParallelStarts,
            Services = source.Services.Select(entry => new RuntimeProfileServiceEntry
            {
                ServiceName = entry.ServiceName,
                Enabled = entry.Enabled,
                DependsOn = entry.DependsOn.ToList()
            }).ToList()
        };
}
