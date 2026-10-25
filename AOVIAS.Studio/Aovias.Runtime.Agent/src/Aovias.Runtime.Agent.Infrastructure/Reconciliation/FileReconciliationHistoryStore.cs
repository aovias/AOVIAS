using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Reconciliation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>
/// The durable <see cref="IReconciliationHistoryStore"/> implementation (Sprint M2.6 D9),
/// persisting to <c>{AuditFolder}/reconciliation/{serviceId}.history.json</c> via the same
/// atomic-write pattern (write to a <c>.tmp</c> file, then <see cref="File.Move(string, string, bool)"/>)
/// <c>RuntimeManifestStore</c> established. A bounded, per-service list (oldest entries are
/// dropped beyond <see cref="MaxRecordsPerService"/>) rather than an unbounded log, so this file
/// cannot grow without limit across a long-running environment.
/// </summary>
public sealed class FileReconciliationHistoryStore : IReconciliationHistoryStore
{
    private const int MaxRecordsPerService = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly ILogger<FileReconciliationHistoryStore> logger;
    private readonly SemaphoreSlim writeGate = new(1, 1);

    public FileReconciliationHistoryStore(IOptionsMonitor<ReleaseManagementSettings> releaseSettings, ILogger<FileReconciliationHistoryStore> logger)
    {
        this.releaseSettings = releaseSettings;
        this.logger = logger;
    }

    public async Task AppendAsync(ReconciliationHistoryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await writeGate.WaitAsync(cancellationToken);
        try
        {
            var path = ResolvePath(record.ServiceId);
            var existing = await ReadAllAsync(path, cancellationToken);
            existing.Add(record);

            var bounded = existing
                .OrderByDescending(r => r.TimestampUtc)
                .Take(MaxRecordsPerService)
                .OrderBy(r => r.TimestampUtc)
                .ToList();

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(bounded, SerializerOptions), cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception exception)
        {
            // Drift history is an audit trail, not a control-flow dependency — a persistence
            // failure must never fail the reconciliation cycle that produced the record it
            // couldn't save (D9's audit trail is best-effort durable, not transactional).
            logger.LogError(exception, "Failed to persist reconciliation history for service {ServiceId}.", record.ServiceId);
        }
        finally
        {
            writeGate.Release();
        }
    }

    public async Task<IReadOnlyList<ReconciliationHistoryRecord>> GetHistoryAsync(string? serviceId = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        if (serviceId is not null)
        {
            var records = await ReadAllAsync(ResolvePath(serviceId), cancellationToken);
            return records.OrderByDescending(r => r.TimestampUtc).Take(limit).ToList();
        }

        var directory = Path.Combine(releaseSettings.CurrentValue.AuditFolder, "reconciliation");
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var all = new List<ReconciliationHistoryRecord>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.history.json"))
        {
            all.AddRange(await ReadAllAsync(file, cancellationToken));
        }

        return all.OrderByDescending(r => r.TimestampUtc).Take(limit).ToList();
    }

    private string ResolvePath(string serviceId)
        => Path.Combine(releaseSettings.CurrentValue.AuditFolder, "reconciliation", $"{serviceId}.history.json");

    private async Task<List<ReconciliationHistoryRecord>> ReadAllAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var records = await JsonSerializer.DeserializeAsync<List<ReconciliationHistoryRecord>>(stream, SerializerOptions, cancellationToken);
            return records ?? [];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read reconciliation history from {Path}.", path);
            return [];
        }
    }
}
