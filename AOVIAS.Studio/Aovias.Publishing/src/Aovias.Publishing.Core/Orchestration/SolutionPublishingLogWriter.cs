using System.Collections.Concurrent;
using System.Text;

namespace Aovias.Publishing.Core.Orchestration;

/// <summary>
/// Writes per-service and summary logs for solution publishing runs.
/// </summary>
public sealed class SolutionPublishingLogWriter : IDisposable
{
    private readonly string logDirectory;
    private readonly ConcurrentDictionary<string, StreamWriter> writers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object summaryLock = new();
    private StreamWriter? summaryWriter;

    public SolutionPublishingLogWriter(string logDirectory)
    {
        this.logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }

    public string LogDirectory => logDirectory;

    public string SummaryLogPath => Path.Combine(logDirectory, "Summary.log");

    public void WriteSummary(string message)
    {
        lock (summaryLock)
        {
            summaryWriter ??= CreateWriter("Summary");
            summaryWriter.WriteLine(FormatLine(message));
            summaryWriter.Flush();
        }
    }

    public void WriteProject(string serviceId, string message)
    {
        var writer = GetProjectWriter(serviceId);
        writer.WriteLine(FormatLine(message));
        writer.Flush();
    }

    public string GetProjectLogPath(string serviceId)
        => Path.Combine(logDirectory, $"{ToLogFileName(serviceId)}.log");

    public void Dispose()
    {
        foreach (var writer in writers.Values)
        {
            writer.Dispose();
        }

        summaryWriter?.Dispose();
    }

    private StreamWriter GetProjectWriter(string serviceId)
    {
        var key = ToLogFileName(serviceId);
        return writers.GetOrAdd(key, CreateWriter);
    }

    private StreamWriter CreateWriter(string fileStem)
        => new(Path.Combine(logDirectory, $"{fileStem}.log"), append: false, Encoding.UTF8) { AutoFlush = true };

    private static string ToLogFileName(string serviceId)
        => serviceId.ToLowerInvariant() switch
        {
            "runtime-agent" => "Runtime",
            "gateway" => "Gateway",
            "compliance-service" => "Compliance",
            "organization-service" => "Organization",
            "identity-service" => "Identity",
            "audit-service" => "Audit",
            "ai" => "AI",
            "portal" => "Portal",
            "knowledge-base" => "KnowledgeBase",
            _ => serviceId.Replace('-', '_')
        };

    private static string FormatLine(string message)
        => $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {message}";
}
