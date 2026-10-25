using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Storage path settings for runtime artifacts.
/// </summary>
public sealed class StorageSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Storage";

    [Required]
    public string DataRoot { get; set; } = string.Empty;

    [Required]
    public string BackupRoot { get; set; } = string.Empty;

    [Required]
    public string TempRoot { get; set; } = string.Empty;
}
