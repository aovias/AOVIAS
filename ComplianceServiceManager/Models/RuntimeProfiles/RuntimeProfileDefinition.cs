namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>Persisted runtime profile definition (configuration only; process state is derived).</summary>
public sealed class RuntimeProfileDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    /// <summary>Maximum concurrent starts within a dependency-ready wave.</summary>
    public int MaxParallelStarts { get; set; } = 2;

    public List<RuntimeProfileServiceEntry> Services { get; set; } = [];
}
