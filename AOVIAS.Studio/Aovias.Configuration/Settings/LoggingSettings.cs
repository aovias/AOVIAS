using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Logging settings for platform components.
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Logging";

    /// <summary>
    /// Default Serilog file name prefix when not configured.
    /// </summary>
    public const string DefaultFileNamePrefix = "runtime-agent-";

    /// <summary>
    /// Default rolling log retention when not configured.
    /// </summary>
    public const int DefaultRetentionDays = 14;

    /// <summary>
    /// Primary log root directory (standard schema).
    /// </summary>
    public string LogRoot { get; set; } = string.Empty;

    /// <summary>
    /// Legacy log directory key retained for backward compatibility.
    /// </summary>
    public string LogDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Optional Serilog file name prefix.
    /// </summary>
    public string FileNamePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Standard rolling log retention in days.
    /// </summary>
    [Range(1, 365)]
    public int RetentionDays { get; set; }

    /// <summary>
    /// Legacy rolling log retention key retained for backward compatibility.
    /// </summary>
    [Range(1, 365)]
    public int RetainedFileCountLimit { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string MinimumLevel { get; set; } = "Information";
}
