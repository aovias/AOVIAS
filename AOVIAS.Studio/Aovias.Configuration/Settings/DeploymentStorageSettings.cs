namespace Aovias.Configuration.Settings;

/// <summary>
/// Canonical deployment filesystem layout. All release and upload paths derive from <see cref="Root"/>.
/// </summary>
public sealed class DeploymentStorageSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DeploymentStorage";

    /// <summary>
    /// Canonical deployment filesystem layout root. Relative segments derive beneath this path.
    /// When omitted, derived from legacy <see cref="ReleaseManagementSettings.ReleaseRoot"/>.
    /// </summary>
    public string Root { get; set; } = string.Empty;

    public string Incoming { get; set; } = "incoming";

    public string Releases { get; set; } = "releases";

    public string Temp { get; set; } = "temp";

    public string Archive { get; set; } = "archive";

    public string Staging { get; set; } = "staging";

    public string Extraction { get; set; } = "extraction";

    public string Active { get; set; } = "active";

    public string Rollback { get; set; } = "rollback";

    public string Audit { get; set; } = "audit";
}
