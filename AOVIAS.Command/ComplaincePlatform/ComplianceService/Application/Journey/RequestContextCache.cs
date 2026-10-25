using System.Collections.Concurrent;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Default request-scoped cache implementation backed by an in-memory dictionary.
/// Service lifetime is Scoped so entries are isolated per request.
/// </summary>
public sealed class RequestContextCache : IRequestContextCache
{
    private readonly ConcurrentDictionary<string, object> _entries = new(StringComparer.Ordinal);

    public bool TryGet<T>(string key, out T? value)
    {
        if (_entries.TryGetValue(key, out var boxed) && boxed is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<T>(string key, T value)
    {
        _entries[key] = value!;
    }

    public void Remove(string key)
    {
        _entries.TryRemove(key, out _);
    }
}

