namespace Aovias.Configuration.Settings;

/// <summary>
/// Explicit platform feature activation flags for modular startup.
/// </summary>
public sealed class PlatformFeatureSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Features";

    public bool Runtime { get; set; } = true;

    public bool ReleaseManagement { get; set; }

    public bool DockerProvider { get; set; }

    public bool SignalR { get; set; }

    public bool Publishing { get; set; }

    public bool Certificates { get; set; }

    public bool Monitoring { get; set; }

    public bool Observability { get; set; }

    public bool Ai { get; set; }
}
