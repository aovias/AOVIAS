namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>Per-member status within a profile start/stop session.</summary>
public enum RuntimeProfileMemberStatus
{
    Pending = 0,
    Reused = 1,
    Starting = 2,
    Ready = 3,
    Failed = 4,
    Blocked = 5,
    Stopping = 6,
    Stopped = 7,
    Skipped = 8
}
