using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Workspace;

/// <summary>
/// Structured publishing logs correlated by session id.
/// </summary>
public sealed class PublishingLogWriter : IPublishingLogWriter
{
    public static readonly IReadOnlyList<string> StandardLogs =
    [
        "Build.log",
        "Docker.log",
        "Contracts.log",
        "Compose.log",
        "Package.log",
        "Verification.log",
        "DRV.log",
        "Summary.log"
    ];

    public void Write(PublishingSession session, string logName, string content)
    {
        var path = GetLogPath(session, logName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"[{DateTimeOffset.UtcNow:O}] {content}{Environment.NewLine}");
    }

    public string GetLogPath(PublishingSession session, string logName)
        => Path.Combine(session.LogsRoot, logName);
}
