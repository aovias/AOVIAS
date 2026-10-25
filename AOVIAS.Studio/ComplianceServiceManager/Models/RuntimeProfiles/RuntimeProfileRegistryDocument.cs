namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>Root document persisted under %LocalAppData%\AoviasStudio\runtime-profiles.json.</summary>
public sealed class RuntimeProfileRegistryDocument
{
    public List<RuntimeProfileDefinition> Profiles { get; set; } = [];
}
