namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>
/// Visualization-only pressure classification for local host resources.
/// Does not drive orchestration or automatic profile decisions (that is D5).
/// </summary>
public enum LocalResourceStatus
{
    Unknown = 0,
    Normal = 1,
    Warning = 2,
    Critical = 3
}
