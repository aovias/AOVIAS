using System.IO;
using System.IO.Compression;
using System.Text.Json;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class LogFileService : ILogFileService
{
    private readonly string compliancePlatformRoot;

    public LogFileService(string compliancePlatformRoot)
    {
        this.compliancePlatformRoot = compliancePlatformRoot;
    }

    public string ResolveLogRoot()
    {
        // Mirror Shared.Logging.LogPathResolver: env override, else <solution>/logs.
        var fromEnv = Environment.GetEnvironmentVariable("COMPLIANCE_LOG_ROOT");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return Path.GetFullPath(fromEnv);
        }

        return Path.Combine(compliancePlatformRoot, "logs");
    }

    public IReadOnlyList<string> ListServices()
    {
        var root = ResolveLogRoot();
        if (!Directory.Exists(root))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(root)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name) && !name!.StartsWith("_archive", StringComparison.OrdinalIgnoreCase))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> ListLogFiles(string serviceName)
    {
        var dir = Path.Combine(ResolveLogRoot(), serviceName);
        if (!Directory.Exists(dir))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(dir, "*.clef")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceLogEntry>> ReadEntriesAsync(string serviceName, int maxEntries, CancellationToken cancellationToken)
    {
        var entries = new List<ServiceLogEntry>();

        // Read newest files first until we have enough entries.
        foreach (var file in ListLogFiles(serviceName))
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.AddRange(await ReadFileAsync(file, cancellationToken));
            if (entries.Count >= maxEntries)
            {
                break;
            }
        }

        return entries
            .OrderByDescending(e => e.Timestamp)
            .Take(maxEntries)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceLogEntry>> GetLatestFailuresAsync(string serviceName, int maxEntries, CancellationToken cancellationToken)
    {
        var entries = await ReadEntriesAsync(serviceName, Math.Max(maxEntries * 10, 500), cancellationToken);
        return entries
            .Where(e => e.IsFailure)
            .OrderByDescending(e => e.Timestamp)
            .Take(maxEntries)
            .ToList();
    }

    public Task<string?> ExportAsync(string serviceName, CancellationToken cancellationToken)
    {
        var files = ListLogFiles(serviceName);
        if (files.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var destination = Path.Combine(
            Path.GetTempPath(),
            $"{serviceName}-logs-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip");

        using (var archive = ZipFile.Open(destination, ZipArchiveMode.Create))
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    // Copy first so a sink holding the file open (shared) does not block zipping.
                    using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var entry = archive.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    source.CopyTo(entryStream);
                }
                catch (IOException)
                {
                    // Skip files that cannot be read.
                }
            }
        }

        return Task.FromResult<string?>(destination);
    }

    private static async Task<List<ServiceLogEntry>> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        var result = new List<ServiceLogEntry>();
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = ParseClef(line, path);
                if (entry is not null)
                {
                    result.Add(entry);
                }
            }
        }
        catch (IOException)
        {
            // File locked or removed mid-read; return what we have.
        }

        return result;
    }

    private static ServiceLogEntry? ParseClef(string line, string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            var timestamp = DateTimeOffset.MinValue;
            if (root.TryGetProperty("@t", out var t) && t.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(t.GetString(), out var parsed))
            {
                timestamp = parsed;
            }

            var level = root.TryGetProperty("@l", out var l) && l.ValueKind == JsonValueKind.String
                ? l.GetString() ?? "Information"
                : "Information";

            var message = GetString(root, "@m") ?? GetString(root, "@mt") ?? string.Empty;
            var exception = GetString(root, "@x");

            return new ServiceLogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message,
                Exception = exception,
                Service = GetString(root, "Service") ?? string.Empty,
                CorrelationId = GetString(root, "CorrelationId"),
                EventCode = GetString(root, "EventCode"),
                Category = GetString(root, "Category"),
                UserId = GetString(root, "UserId"),
                SourceFile = Path.GetFileName(sourceFile)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }
}
