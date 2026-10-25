using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Models.ServiceInstances;

namespace ComplianceServiceManager.Services.ServiceInstances;

/// <summary>
/// Persists stable service-instance identity under %LocalAppData%\AoviasStudio\service-instances.json.
/// Runtime/process/port telemetry is never authoritative here.
/// </summary>
public sealed class ServiceInstanceRegistry
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
    private ServiceInstanceRegistryDocument document;

    public ServiceInstanceRegistry(string? storePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        this.storePath = storePath ?? Path.Combine(root, "service-instances.json");
        document = Load();
    }

    public string StorePath => storePath;

    public IReadOnlyList<ServiceInstanceDefinition> GetAll()
    {
        lock (gate)
        {
            return document.Instances.Select(Clone).ToList();
        }
    }

    public ServiceInstanceDefinition? GetById(string instanceId)
    {
        lock (gate)
        {
            var match = document.Instances.FirstOrDefault(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : Clone(match);
        }
    }

    public IReadOnlyList<ServiceInstanceDefinition> GetByServiceId(string serviceId)
    {
        lock (gate)
        {
            return document.Instances
                .Where(item => string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase))
                .Select(Clone)
                .ToList();
        }
    }

    public void Upsert(ServiceInstanceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.InstanceId))
        {
            throw new ArgumentException("InstanceId is required.", nameof(definition));
        }

        lock (gate)
        {
            var index = document.Instances.FindIndex(existing =>
                string.Equals(existing.InstanceId, definition.InstanceId, StringComparison.OrdinalIgnoreCase));
            var clone = Clone(definition);
            if (index >= 0)
            {
                document.Instances[index] = clone;
            }
            else
            {
                document.Instances.Add(clone);
            }

            Persist();
        }
    }

    public bool Remove(string instanceId)
    {
        lock (gate)
        {
            var removed = document.Instances.RemoveAll(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return false;
            }

            Persist();
            return true;
        }
    }

    private ServiceInstanceRegistryDocument Load()
    {
        try
        {
            if (!File.Exists(storePath))
            {
                return new ServiceInstanceRegistryDocument();
            }

            var loaded = JsonSerializer.Deserialize<ServiceInstanceRegistryDocument>(
                File.ReadAllText(storePath),
                JsonOptions);
            return loaded ?? new ServiceInstanceRegistryDocument();
        }
        catch
        {
            return new ServiceInstanceRegistryDocument();
        }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(storePath, json);
    }

    private static ServiceInstanceDefinition Clone(ServiceInstanceDefinition source)
        => new()
        {
            InstanceId = source.InstanceId,
            ServiceId = source.ServiceId,
            ServiceName = source.ServiceName,
            ExecutionScope = source.ExecutionScope,
            Ownership = source.Ownership,
            WorkspaceId = source.WorkspaceId,
            AgentSessionId = source.AgentSessionId,
            RuntimeProfileId = source.RuntimeProfileId,
            CreatedAt = source.CreatedAt
        };
}
