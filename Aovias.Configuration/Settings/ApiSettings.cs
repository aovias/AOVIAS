using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// API endpoint and HTTP client settings.
/// </summary>
public sealed class ApiSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Api";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string PublicUrl { get; set; } = string.Empty;

    [Required]
    public string InternalUrl { get; set; } = string.Empty;

    [Required]
    public string HealthEndpoint { get; set; } = string.Empty;

    public string PathBase { get; set; } = string.Empty;

    [Range(1, 600)]
    public int TimeoutSeconds { get; set; }

    [Range(0, 10)]
    public int RetryCount { get; set; }
}
