using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Reverse proxy settings reserved for future nginx management.
/// </summary>
public sealed class NginxSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Nginx";

    public bool Enabled { get; set; }

    [Required]
    public string ConfigRoot { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = string.Empty;

    [Required]
    public string Executable { get; set; } = "nginx";

    [Required(AllowEmptyStrings = true)]
    public string PublicPathPrefix { get; set; } = string.Empty;

    [Required]
    public string UpstreamHost { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int UpstreamPort { get; set; } = 7080;

    [Required]
    public string SiteTemplatePath { get; set; } = string.Empty;

    [Required]
    public string ServerName { get; set; } = string.Empty;

    [Required]
    public string ReloadCommand { get; set; } = "-s reload";

    [Range(1, 256)]
    public int UploadBodyLimitMegabytes { get; set; } = 16;

    [Range(30, 3600)]
    public int UploadTimeoutSeconds { get; set; } = 600;
}
