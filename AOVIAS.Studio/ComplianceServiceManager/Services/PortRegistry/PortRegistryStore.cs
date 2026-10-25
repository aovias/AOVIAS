using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Models.PortRegistry;

namespace ComplianceServiceManager.Services.PortRegistry;

/// <summary>
/// Persists stable port→ServiceInstance assignments under %LocalAppData%\AoviasStudio\port-registry.json.
/// OS occupancy is never stored as authoritative.
/// </summary>
public sealed class PortRegistryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string storePath;
    private readonly object gate = new();
    private PortRegistryDocument document;

    public PortRegistryStore(string? storePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        this.storePath = storePath ?? Path.Combine(root, "port-registry.json");
        document = Load();
    }

    public string StorePath => storePath;

    public IReadOnlyList<PortAllocationDefinition> GetAll()
    {
        lock (gate)
        {
            return document.Allocations.Select(Clone).ToList();
        }
    }

    public PortAllocationDefinition? GetByPort(int port)
    {
        lock (gate)
        {
            var match = document.Allocations.FirstOrDefault(item => item.Port == port);
            return match is null ? null : Clone(match);
        }
    }

    public PortAllocationDefinition? GetByInstanceId(string instanceId)
    {
        lock (gate)
        {
            var match = document.Allocations.FirstOrDefault(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : Clone(match);
        }
    }

    public void Upsert(PortAllocationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var byPort = document.Allocations.FindIndex(item => item.Port == definition.Port);
            var byInstance = document.Allocations.FindIndex(item =>
                string.Equals(item.InstanceId, definition.InstanceId, StringComparison.OrdinalIgnoreCase));

            // Reject if port owned by different instance
            if (byPort >= 0
                && !string.Equals(document.Allocations[byPort].InstanceId, definition.InstanceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Port {definition.Port} is already assigned to another service instance.");
            }

            var clone = Clone(definition);
            clone.UpdatedAt = DateTimeOffset.UtcNow;
            if (byInstance >= 0)
            {
                // Moving instance to a new port: remove old port row if different
                if (document.Allocations[byInstance].Port != definition.Port)
                {
                    document.Allocations.RemoveAt(byInstance);
                    byPort = document.Allocations.FindIndex(item => item.Port == definition.Port);
                }
                else
                {
                    document.Allocations[byInstance] = clone;
                    Persist();
                    return;
                }
            }

            if (byPort >= 0)
            {
                document.Allocations[byPort] = clone;
            }
            else
            {
                document.Allocations.Add(clone);
            }

            Persist();
        }
    }

    public bool RemoveByPort(int port)
    {
        lock (gate)
        {
            var removed = document.Allocations.RemoveAll(item => item.Port == port);
            if (removed == 0)
            {
                return false;
            }

            Persist();
            return true;
        }
    }

    public bool RemoveByInstanceId(string instanceId)
    {
        lock (gate)
        {
            var removed = document.Allocations.RemoveAll(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return false;
            }

            Persist();
            return true;
        }
    }

    private PortRegistryDocument Load()
    {
        try
        {
            if (!File.Exists(storePath))
            {
                return new PortRegistryDocument();
            }

            var loaded = JsonSerializer.Deserialize<PortRegistryDocument>(File.ReadAllText(storePath), JsonOptions);
            return loaded ?? new PortRegistryDocument();
        }
        catch
        {
            return new PortRegistryDocument();
        }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(storePath, json);
    }

    private static PortAllocationDefinition Clone(PortAllocationDefinition source)
        => new()
        {
            Port = source.Port,
            InstanceId = source.InstanceId,
            ServiceId = source.ServiceId,
            WorkspaceId = source.WorkspaceId,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            Notes = source.Notes
        };
}
