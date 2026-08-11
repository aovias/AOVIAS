namespace ComplianceServiceManager.Models.Runtime;

/// <summary>
/// Represents a runtime event entry for the operational event log.
/// </summary>
public sealed class RuntimeEvent
{
    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the event severity label.
    /// </summary>
    public string Level { get; init; } = "Info";

    /// <summary>
    /// Gets or sets the event message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the formatted timestamp for display.
    /// </summary>
    public string DisplayTimestamp => Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
}
