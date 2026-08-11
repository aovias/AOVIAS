namespace ComplianceServiceManager.Models;

public sealed class LogEntryModel
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public LogLevel Level { get; init; }

    public string Source { get; init; } = "System";

    public string Message { get; init; } = string.Empty;

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");

    public string LevelText => Level.ToString().ToUpperInvariant();

    public string DisplayText => $"[{Timestamp:HH:mm:ss}] [{Level.ToString().ToUpperInvariant()}] [{Source}] {Message}";
}
