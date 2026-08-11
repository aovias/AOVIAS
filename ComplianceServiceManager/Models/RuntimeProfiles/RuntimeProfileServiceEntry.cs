namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>Persisted membership of one service inside a runtime profile.</summary>
public sealed class RuntimeProfileServiceEntry
{
    public string ServiceName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    /// <summary>Other profile service names that must be ready before this service starts.</summary>
    public List<string> DependsOn { get; set; } = [];
}
