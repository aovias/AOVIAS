namespace ComplianceServiceManager.Models.ResourceIntelligence;

/// <summary>Advisory resource pressure classification. Not an OS/safety guarantee.</summary>
public enum ResourcePressureLevel
{
    Normal = 0,
    Warning = 1,
    High = 2,
    Critical = 3,
    Unknown = 4
}

/// <summary>Comparative runtime profile resource class (not an absolute GB claim).</summary>
public enum RuntimeProfileResourceClass
{
    Light = 0,
    Medium = 1,
    Heavy = 2,
    Unknown = 3
}

public enum ResourceRecommendationSeverity
{
    Info = 0,
    Warning = 1,
    High = 2,
    Critical = 3
}

public enum ResourceRecommendationKind
{
    LowMemory = 0,
    ProfileTooHeavy = 1,
    MultipleAgentPressure = 2,
    CpuPressure = 3,
    DiskPressure = 4,
    PreferLighterProfile = 5,
    CapacityOk = 6
}
