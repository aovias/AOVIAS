using System.Text.Json;

namespace ComplianceServiceManager.Operations;

/// <summary>
/// Durable, file-backed operation history — one JSON record per finished run plus its exported
/// structured-log text file, under <c>%LocalAppData%\AoviasStudio\operations\</c> by default. This
/// is the telemetry/history substrate every operation kind shares; nothing here is Bootstrap- (or
/// any other operation-) specific.
/// </summary>
public sealed class OperationHistoryStore
{
    private readonly string rootDirectory;

    public OperationHistoryStore(string? rootDirectory = null)
    {
        this.rootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AoviasStudio",
            "operations");
    }

    public string RootDirectory => rootDirectory;

    /// <summary>Writes the exported log file and the JSON history record for a finished operation. Called by <see cref="OperationEngine"/> exactly once, when a run reaches a terminal state.</summary>
    public async Task RecordAsync(OperationInstance instance, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(rootDirectory);
        var logsDirectory = Path.Combine(rootDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        var logPath = Path.Combine(logsDirectory, $"{instance.StartedUtc:yyyyMMddHHmmss}-{instance.RunId}.log");
        var logLines = instance.Stages
            .SelectMany(stage => stage.Logs)
            .OrderBy(log => log.Timestamp)
            .Select(log => $"[{log.Timestamp:O}] [{log.Severity}] [{log.StageName}] {log.Message}")
            .ToList();
        await File.WriteAllLinesAsync(logPath, logLines, cancellationToken);

        var record = OperationHistoryRecord.FromInstance(instance, logPath);
        var recordPath = Path.Combine(rootDirectory, $"{instance.StartedUtc:yyyyMMddHHmmss}-{Sanitize(instance.Descriptor.TargetIdentity)}-{instance.RunId}.json");
        await using var stream = File.Create(recordPath);
        await JsonSerializer.SerializeAsync(stream, record, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    /// <summary>Reads back history records, most recent first, optionally filtered by target identity and/or operation kind. A record file that fails to parse (corrupted/partial write) is skipped rather than failing the whole read.</summary>
    public async Task<IReadOnlyList<OperationHistoryRecord>> GetHistoryAsync(
        string? targetIdentity = null,
        string? kind = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return [];
        }

        var records = new List<OperationHistoryRecord>();
        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*.json"))
        {
            OperationHistoryRecord? record;
            try
            {
                await using var stream = File.OpenRead(file);
                record = await JsonSerializer.DeserializeAsync<OperationHistoryRecord>(stream, cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                continue;
            }

            if (record is null)
            {
                continue;
            }

            if (targetIdentity is not null && !string.Equals(record.TargetIdentity, targetIdentity, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (kind is not null && !string.Equals(record.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            records.Add(record);
        }

        return records.OrderByDescending(record => record.StartedUtc).ToList();
    }

    private static string Sanitize(string value) => new(value.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
}
