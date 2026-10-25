using System.Collections.Concurrent;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Proactive;

public sealed class ProactiveSuggestionItem
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public AIResponse Suggestion { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IProactiveSuggestionStore
{
    void Add(ProactiveSuggestionItem item);
    IReadOnlyList<ProactiveSuggestionItem> GetForEntity(string entityType, string entityId, int maxCount = 20);
}

public sealed class ProactiveSuggestionStore : IProactiveSuggestionStore
{
    private readonly ConcurrentQueue<ProactiveSuggestionItem> _items = new();

    public void Add(ProactiveSuggestionItem item)
    {
        _items.Enqueue(item);
        while (_items.Count > 500)
        {
            _items.TryDequeue(out _);
        }
    }

    public IReadOnlyList<ProactiveSuggestionItem> GetForEntity(string entityType, string entityId, int maxCount = 20)
    {
        return _items
            .Where(x => string.Equals(x.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.EntityId, entityId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Max(1, maxCount))
            .ToList();
    }
}
