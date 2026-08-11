namespace ComplianceService.Application.Journey;

/// <summary>
/// Scoped in-request cache used to share expensive read-model builds
/// (for example journey workspace context) across multiple service calls
/// within the same HTTP request.
/// </summary>
public interface IRequestContextCache
{
    bool TryGet<T>(string key, out T? value);

    void Set<T>(string key, T value);

    void Remove(string key);
}

