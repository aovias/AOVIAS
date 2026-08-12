using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceServiceManager.Models.AgentSessions;

namespace ComplianceServiceManager.Services.AgentSessions;

/// <summary>Persists development-agent sessions under %LocalAppData%\AoviasStudio\agent-sessions.json.</summary>
public sealed class AgentSessionRegistry
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
    private AgentSessionRegistryDocument document;

    public AgentSessionRegistry(string? storePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio");
        Directory.CreateDirectory(root);
        this.storePath = storePath ?? Path.Combine(root, "agent-sessions.json");
        document = Load();
    }

    public string StorePath => storePath;

    public IReadOnlyList<AgentSessionDefinition> GetAll()
    {
        lock (gate)
        {
            return document.Sessions.Select(Clone).ToList();
        }
    }

    public AgentSessionDefinition? GetById(string id)
    {
        lock (gate)
        {
            var match = document.Sessions.FirstOrDefault(session =>
                string.Equals(session.Id, id, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : Clone(match);
        }
    }

    public void Upsert(AgentSessionDefinition definition)
    {
        lock (gate)
        {
            var index = document.Sessions.FindIndex(existing =>
                string.Equals(existing.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
            var clone = Clone(definition);
            if (index >= 0)
            {
                document.Sessions[index] = clone;
            }
            else
            {
                document.Sessions.Add(clone);
            }

            Persist();
        }
    }

    public bool Remove(string id)
    {
        lock (gate)
        {
            var removed = document.Sessions.RemoveAll(session =>
                string.Equals(session.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return false;
            }

            Persist();
            return true;
        }
    }

    private AgentSessionRegistryDocument Load()
    {
        try
        {
            if (File.Exists(storePath))
            {
                var loaded = JsonSerializer.Deserialize<AgentSessionRegistryDocument>(
                    File.ReadAllText(storePath),
                    JsonOptions);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
        }

        return new AgentSessionRegistryDocument();
    }

    private void Persist()
    {
        try
        {
            File.WriteAllText(storePath, JsonSerializer.Serialize(document, JsonOptions));
        }
        catch
        {
        }
    }

    private static AgentSessionDefinition Clone(AgentSessionDefinition source)
        => new()
        {
            Id = source.Id,
            AgentType = source.AgentType,
            Title = source.Title,
            Description = source.Description,
            TaskId = source.TaskId,
            WorkspaceId = source.WorkspaceId,
            ExpectedBranch = source.ExpectedBranch,
            RuntimeProfileId = source.RuntimeProfileId,
            PullRequestId = source.PullRequestId,
            Status = source.Status,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            StartedAt = source.StartedAt,
            CompletedAt = source.CompletedAt,
            LastObservedAt = source.LastObservedAt
        };
}
