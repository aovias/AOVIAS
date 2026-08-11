using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>UI-facing per-member row during a profile session.</summary>
public sealed partial class RuntimeProfileMemberItem : ObservableObject
{
    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private RuntimeProfileMemberStatus status = RuntimeProfileMemberStatus.Pending;

    [ObservableProperty]
    private string statusDisplay = "Pending";

    [ObservableProperty]
    private string? message;

    [ObservableProperty]
    private bool ownedByProfile;

    [ObservableProperty]
    private bool reused;

    public void Apply(RuntimeProfileMemberStatus next, string? message = null, bool? owned = null, bool? reused = null)
    {
        Status = next;
        StatusDisplay = next switch
        {
            RuntimeProfileMemberStatus.Pending => "Pending",
            RuntimeProfileMemberStatus.Reused => "Reused",
            RuntimeProfileMemberStatus.Starting => "Starting",
            RuntimeProfileMemberStatus.Ready => "Ready",
            RuntimeProfileMemberStatus.Failed => "Failed",
            RuntimeProfileMemberStatus.Blocked => "Blocked",
            RuntimeProfileMemberStatus.Stopping => "Stopping",
            RuntimeProfileMemberStatus.Stopped => "Stopped",
            RuntimeProfileMemberStatus.Skipped => "Skipped",
            _ => next.ToString()
        };
        Message = message;
        if (owned is not null)
        {
            OwnedByProfile = owned.Value;
        }

        if (reused is not null)
        {
            Reused = reused.Value;
        }
    }
}
