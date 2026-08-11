using System.Text;

namespace ComplianceServiceManager.Models;

/// <summary>
/// A single structured log event parsed from a service's CLEF (compact JSON) log file.
/// </summary>
public sealed class ServiceLogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.MinValue;

    public string Level { get; init; } = "Information";

    public string Service { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Exception { get; init; }

    public string? CorrelationId { get; init; }

    public string? EventCode { get; init; }

    public string? Category { get; init; }

    public string? UserId { get; init; }

    public string SourceFile { get; init; } = string.Empty;

    public string DisplayTime => Timestamp == DateTimeOffset.MinValue
        ? "-"
        : Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");

    public string LevelText => Level.ToUpperInvariant();

    public bool IsFailure =>
        Level.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
        Level.Equals("Fatal", StringComparison.OrdinalIgnoreCase);

    public string DisplayText =>
        $"[{DisplayTime}] [{LevelText}] [{EventCode ?? "-"}] {Message}{(Exception is null ? string.Empty : Environment.NewLine + Exception)}";

    /// <summary>
    /// First line / type of the exception, used to identify the failure independent of
    /// variable details like ids, timestamps, or counts.
    /// </summary>
    public string ExceptionType
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Exception))
            {
                return string.Empty;
            }

            var firstLine = Exception.Split('\n', '\r').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;
            var colon = firstLine.IndexOf(':');
            return (colon > 0 ? firstLine[..colon] : firstLine).Trim();
        }
    }

    /// <summary>
    /// A stable signature for the failure (EventCode + exception type + normalized message head)
    /// so the same recurring error can be recognized across log files and fix sessions.
    /// </summary>
    public string Fingerprint
    {
        get
        {
            var normalizedMessage = NormalizeForFingerprint(Message);
            var head = normalizedMessage.Length > 90 ? normalizedMessage[..90] : normalizedMessage;
            return $"{EventCode ?? "-"}|{ExceptionType}|{head}";
        }
    }

    private static string NormalizeForFingerprint(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Drop digits and hex characters (collapses ids/GUIDs/timestamps), lower-case the rest,
        // and collapse runs of whitespace so identical errors with different ids match.
        var builder = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            lastWasSpace = false;
            if (char.IsDigit(ch) || ch is '-' or '_')
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString().Trim();
    }
}
