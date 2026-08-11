using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Timeline;

/// <summary>
/// Immutable runtime timeline backed by an in-memory ring buffer.
/// </summary>
public sealed class RuntimeTimelineStore : IRuntimeTimelineStore
{
    private readonly object sync = new();
    private readonly LinkedList<RuntimeTimelineEntryDto> entries = new();
    private readonly IOptionsMonitor<ObservabilitySettings> observabilitySettings;

    public RuntimeTimelineStore(IOptionsMonitor<ObservabilitySettings> observabilitySettings)
    {
        this.observabilitySettings = observabilitySettings;
    }

    /// <inheritdoc />
    public void Append(RuntimeTimelineEntryDto entry)
    {
        lock (sync)
        {
            entries.AddFirst(entry);
            var maxEntries = Math.Max(100, observabilitySettings.CurrentValue.TimelineMaxEntries);
            while (entries.Count > maxEntries)
            {
                entries.RemoveLast();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RuntimeTimelineEntryDto> Query(string? searchTerm = null, string? category = null, int limit = 100)
    {
        lock (sync)
        {
            IEnumerable<RuntimeTimelineEntryDto> query = entries;
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(entry => entry.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(entry =>
                    entry.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (entry.ContainerName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (entry.WorkflowId?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (entry.ReleaseId?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (entry.CorrelationId?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (entry.OperationId?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return query.Take(Math.Max(1, limit)).ToList();
        }
    }
}
