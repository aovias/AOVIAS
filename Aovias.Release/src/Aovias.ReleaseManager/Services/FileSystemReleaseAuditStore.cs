using System.Text.Json;
using Aovias.Configuration.Deployment;
using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Persists release audit events to the configured audit folder.
/// </summary>
public sealed class FileSystemReleaseAuditStore : IReleaseAuditStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IDeploymentPathResolver pathResolver;
    private readonly IReleaseRepository releaseRepository;
    private readonly ILogger<FileSystemReleaseAuditStore> logger;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public FileSystemReleaseAuditStore(
        IDeploymentPathResolver pathResolver,
        IReleaseRepository releaseRepository,
        ILogger<FileSystemReleaseAuditStore> logger)
    {
        this.pathResolver = pathResolver;
        this.releaseRepository = releaseRepository;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await writeLock.WaitAsync(cancellationToken);

        try
        {
            var auditRoot = pathResolver.AuditRoot;
            pathResolver.EnsureDirectory(auditRoot);
            pathResolver.EnsureDirectory(Path.Combine(auditRoot, "events"));
            pathResolver.EnsureDirectory(Path.Combine(auditRoot, "deployments"));

            var eventPath = Path.Combine(auditRoot, "events", $"{auditEvent.EventId}.json");
            await File.WriteAllTextAsync(eventPath, JsonSerializer.Serialize(auditEvent, SerializerOptions), cancellationToken);

            var deploymentId = auditEvent.DeploymentId ?? auditEvent.ReleaseId;
            var deploymentPath = Path.Combine(auditRoot, "deployments", $"{deploymentId}.json");
            var events = await LoadDeploymentEventsAsync(deploymentPath, cancellationToken);
            events.Add(auditEvent);
            await File.WriteAllTextAsync(deploymentPath, JsonSerializer.Serialize(events, SerializerOptions), cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEvent>> GetByReleaseIdAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allEvents = await LoadAllEventsAsync(cancellationToken);
        return allEvents
            .Where(entry => string.Equals(entry.ReleaseId, releaseId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Gets all audit events ordered by timestamp descending.
    /// </summary>
    public async Task<IReadOnlyList<AuditEvent>> GetAllAsync(CancellationToken cancellationToken = default)
        => await LoadAllEventsAsync(cancellationToken);

    /// <summary>
    /// Gets audit events for a deployment identifier.
    /// </summary>
    public async Task<IReadOnlyList<AuditEvent>> GetDeploymentEventsAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deploymentPath = Path.Combine(pathResolver.AuditRoot, "deployments", $"{deploymentId}.json");
        return await LoadDeploymentEventsAsync(deploymentPath, cancellationToken);
    }

    /// <summary>
    /// Builds deployment history entries from persisted audit events.
    /// </summary>
    public async Task<IReadOnlyList<ReleaseHistoryEntry>> SearchHistoryAsync(
        ReleaseHistorySearchCriteria? criteria,
        CancellationToken cancellationToken = default)
    {
        var events = await LoadAllEventsAsync(cancellationToken);
        var deploymentGroups = events
            .GroupBy(entry => entry.DeploymentId ?? entry.ReleaseId, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildHistoryEntry(group.ToList()))
            .Where(entry => entry is not null)
            .Cast<ReleaseHistoryEntry>()
            .OrderByDescending(entry => entry.Timestamp)
            .ToList();

        if (criteria is null)
        {
            return deploymentGroups;
        }

        return deploymentGroups
            .Where(entry => criteria.Runtime is null || string.Equals(entry.Runtime, criteria.Runtime, StringComparison.OrdinalIgnoreCase))
            .Where(entry => criteria.State is null || entry.State == criteria.State)
            .Where(entry => criteria.Operator is null || string.Equals(entry.Operator, criteria.Operator, StringComparison.OrdinalIgnoreCase))
            .Where(entry => criteria.From is null || entry.Timestamp >= criteria.From)
            .Where(entry => criteria.To is null || entry.Timestamp <= criteria.To)
            .ToList();
    }

    private async Task<List<AuditEvent>> LoadDeploymentEventsAsync(string deploymentPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(deploymentPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(deploymentPath);
        var events = await JsonSerializer.DeserializeAsync<List<AuditEvent>>(stream, SerializerOptions, cancellationToken);
        return events ?? [];
    }

    private async Task<IReadOnlyList<AuditEvent>> LoadAllEventsAsync(CancellationToken cancellationToken)
    {
        var auditRoot = pathResolver.AuditRoot;
        var eventsDirectory = Path.Combine(auditRoot, "events");
        if (!Directory.Exists(eventsDirectory))
        {
            return [];
        }

        var events = new List<AuditEvent>();
        foreach (var file in Directory.GetFiles(eventsDirectory, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                var auditEvent = await JsonSerializer.DeserializeAsync<AuditEvent>(stream, SerializerOptions, cancellationToken);
                if (auditEvent is not null)
                {
                    events.Add(auditEvent);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to read audit event file {AuditFile}", file);
            }
        }

        return events.OrderByDescending(entry => entry.Timestamp).ToList();
    }

    private ReleaseHistoryEntry? BuildHistoryEntry(IReadOnlyList<AuditEvent> events)
    {
        if (events.Count == 0)
        {
            return null;
        }

        var ordered = events.OrderBy(entry => entry.Timestamp).ToList();
        var first = ordered[0];
        var last = ordered[^1];
        var started = ordered.FirstOrDefault(entry => entry.EventType == AuditEventType.DeploymentStarted);
        var completed = ordered.LastOrDefault(entry =>
            entry.EventType is AuditEventType.DeploymentCompleted or AuditEventType.RollbackCompleted or AuditEventType.PipelineStepFailed);
        var wasRollback = ordered.Any(entry =>
            entry.EventType is AuditEventType.RollbackStarted or AuditEventType.RollbackCompleted);

        var state = ReleaseState.Completed;
        if (ordered.Any(entry => entry.EventType == AuditEventType.PipelineStepFailed))
        {
            state = ReleaseState.Failed;
        }
        else if (wasRollback)
        {
            state = ReleaseState.RolledBack;
        }

        return new ReleaseHistoryEntry
        {
            ReleaseId = first.ReleaseId,
            PackageId = first.PackageId,
            PackageVersion = first.Metadata.GetValueOrDefault("packageVersion") ?? string.Empty,
            Runtime = first.Metadata.GetValueOrDefault("runtime") ?? "runtime",
            Environment = first.Metadata.GetValueOrDefault("environment") ?? "production",
            State = state,
            Timestamp = last.Timestamp,
            Summary = last.Message,
            DeploymentId = first.DeploymentId ?? first.ReleaseId,
            Operator = first.Actor,
            StartedAt = started?.Timestamp ?? first.Timestamp,
            CompletedAt = completed?.Timestamp,
            Duration = started is not null && completed is not null ? completed.Timestamp - started.Timestamp : null,
            WasRollback = wasRollback,
            CorrelationId = first.CorrelationId
        };
    }
}
