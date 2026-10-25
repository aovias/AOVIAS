using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// SignalR real-time communication settings.
/// </summary>
public sealed class SignalRSettings
{
    public const string SectionName = "SignalR";

    public bool Enabled { get; set; }

    [Required]
    public string HubPath { get; set; } = string.Empty;

    [Range(1, 600)]
    public int KeepAliveSeconds { get; set; }

    [Range(1, 600)]
    public int ClientTimeoutSeconds { get; set; }
}
