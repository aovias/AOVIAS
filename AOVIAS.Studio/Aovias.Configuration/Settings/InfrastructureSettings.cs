using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Infrastructure networking and public endpoint settings.
/// </summary>
public sealed class InfrastructureSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Infrastructure";

    [Required]
    public string PublicIp { get; set; } = string.Empty;

    [Required]
    public string Domain { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int HttpsPort { get; set; }

    [Range(1, 65535)]
    public int HttpPort { get; set; }

    public bool SslEnabled { get; set; }
}
