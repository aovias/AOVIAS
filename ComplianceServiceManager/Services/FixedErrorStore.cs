using System.IO;
using System.Text.Json;

namespace ComplianceServiceManager.Services;

/// <summary>
/// JSON-backed implementation of <see cref="IFixedErrorStore"/>. State is persisted under
/// the manager's Diagnostics folder so fixed-error awareness survives across app restarts.
/// </summary>
public sealed class FixedErrorStore : IFixedErrorStore
{
    private readonly string filePath;
    private readonly object gate = new();
    private readonly Dictionary<string, ServiceFixState> state = new(StringComparer.OrdinalIgnoreCase);

    public FixedErrorStore()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Diagnostics", "Analytics");
        Directory.CreateDirectory(directory);
        filePath = Path.Combine(directory, "fixed-errors.json");
        Load();
    }

    public DateTimeOffset? GetLastFixedAt(string serviceName)
    {
        lock (gate)
        {
            return state.TryGetValue(serviceName, out var entry) ? entry.LastFixedAt : null;
        }
    }

    public IReadOnlyCollection<string> GetResolvedFingerprints(string serviceName)
    {
        lock (gate)
        {
            return state.TryGetValue(serviceName, out var entry)
                ? new HashSet<string>(entry.Fingerprints, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }
    }

    public void MarkResolved(string serviceName, IEnumerable<string> fingerprints, DateTimeOffset fixedAt)
    {
        lock (gate)
        {
            if (!state.TryGetValue(serviceName, out var entry))
            {
                entry = new ServiceFixState();
                state[serviceName] = entry;
            }

            foreach (var fingerprint in fingerprints)
            {
                if (!string.IsNullOrWhiteSpace(fingerprint))
                {
                    entry.Fingerprints.Add(fingerprint);
                }
            }

            // Cap stored fingerprints so the file does not grow without bound.
            if (entry.Fingerprints.Count > 500)
            {
                entry.Fingerprints = entry.Fingerprints
                    .Skip(entry.Fingerprints.Count - 500)
                    .ToList();
            }

            entry.LastFixedAt = fixedAt;
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, ServiceFixState>>(json);
            if (loaded is null)
            {
                return;
            }

            foreach (var (key, value) in loaded)
            {
                state[key] = value;
            }
        }
        catch (Exception)
        {
            // Corrupt or unreadable state must never block the manager; start fresh.
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            // Persistence is best-effort; an IO failure should not break the fix flow.
        }
    }

    private sealed class ServiceFixState
    {
        public DateTimeOffset? LastFixedAt { get; set; }

        public List<string> Fingerprints { get; set; } = new();
    }
}
