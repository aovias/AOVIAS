using System.Collections.Concurrent;
using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Enums;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// In-memory immutable audit store reserved for future persistence integration.
/// </summary>
public sealed class InMemoryReleaseAuditStore : Abstractions.IReleaseAuditStore
{
    private readonly ConcurrentDictionary<string, List<AuditEvent>> events = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bucket = events.GetOrAdd(auditEvent.ReleaseId, _ => []);
        lock (bucket)
        {
            bucket.Add(auditEvent);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AuditEvent>> GetByReleaseIdAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!events.TryGetValue(releaseId, out var bucket))
        {
            return Task.FromResult<IReadOnlyList<AuditEvent>>([]);
        }

        lock (bucket)
        {
            IReadOnlyList<AuditEvent> copy = bucket.OrderBy(entry => entry.Timestamp).ToList();
            return Task.FromResult(copy);
        }
    }

    /// <summary>
    /// Gets all audit events for history views.
    /// </summary>
    public IReadOnlyList<AuditEvent> GetAll()
    {
        return events.Values
            .SelectMany(list =>
            {
                lock (list)
                {
                    return list.ToList();
                }
            })
            .OrderByDescending(entry => entry.Timestamp)
            .ToList();
    }
}
